// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RainIndices.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Defines the RainIndices type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AudioAnalysisTools.Indices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.StandardSpectrograms;

    using TowseyLibrary;

    public static class RainIndices
    {
        public const string header_rain = "Rain";
        public const string header_cicada = "Cicadas";
        public const string header_negative = "none";

        /// <summary>
        /// a set of indices derived from each recording.
        /// </summary>
        public struct RainStruct
        {
            public double Snr, BgNoise, Activity, Spikes, AvSig_dB, TemporalEntropy; //amplitude indices
            public double LowFreqCover, MidFreqCover, HiFreqCover, SpectralEntropy;  //, entropyOfVarianceSpectrum; //spectral indices
            public double ACI;

            public RainStruct(double _snr, double _bgNoise, double _avSig_dB, double _activity, double _spikes,
                            double _entropyAmp, double _hiFreqCover, double _midFreqCover, double _lowFreqCover,
                            double _entropyOfAvSpectrum, double _ACI)
            {
                this.Snr = _snr;
                this.BgNoise = _bgNoise;
                this.Activity = _activity;
                this.Spikes = _spikes;
                this.AvSig_dB = _avSig_dB;
                this.TemporalEntropy = _entropyAmp;
                this.HiFreqCover = _hiFreqCover;
                this.MidFreqCover = _midFreqCover;
                this.LowFreqCover = _lowFreqCover;
                this.SpectralEntropy = _entropyOfAvSpectrum;
                this.ACI = _ACI;
            }
        } //struct Indices

        /// <summary>
        ///
        /// </summary>
        /// <param name="signalEnvelope">envelope of the original signal.</param>
        /// <param name="spectrogram">the original amplitude spectrum BUT noise reduced.</param>
        /// <param name="binWidth">derived from original nyquist and window/2.</param>
        public static Dictionary<string, double> GetIndices(double[] signalEnvelope, TimeSpan audioDuration, TimeSpan frameStepDuration, double[,] spectrogram, int lowFreqBound, int midFreqBound, double binWidth)
        {
            int chunkDuration = 10; //seconds - assume that signal is not less than one minute duration

            double framesPerSecond = 1 / frameStepDuration.TotalSeconds;
            int chunkCount = (int)Math.Round(audioDuration.TotalSeconds / chunkDuration);
            int framesPerChunk = (int)(chunkDuration * framesPerSecond);
            int nyquistBin = spectrogram.GetLength(1);

            string[] classifications = new string[chunkCount];

            //get acoustic indices and convert to rain indices.
            var sb = new StringBuilder();
            for (int i = 0; i < chunkCount; i++)
            {
                int startSecond = i * chunkDuration;
                int start = (int)(startSecond * framesPerSecond);
                int end = start + framesPerChunk;
                if (end >= signalEnvelope.Length)
                {
                    end = signalEnvelope.Length - 1;
                }

                double[] chunkSignal = DataTools.Subarray(signalEnvelope, start, framesPerChunk);
                if (chunkSignal.Length < 50)
                {
                    continue;  //an arbitrary minimum length
                }

                double[,] chunkSpectro = DataTools.Submatrix(spectrogram, start, 1, end, nyquistBin - 1);

                RainStruct rainIndices = Get10SecondIndices(chunkSignal, chunkSpectro, lowFreqBound, midFreqBound, frameStepDuration, binWidth);
                string classification = ConvertAcousticIndices2Classifcations(rainIndices);
                classifications[i] = classification;

                //write indices and classification info to console
                string separator = ",";
                string line = string.Format("{1:d2}{0} {2:d2}{0} {3:f1}{0} {4:f1}{0} {5:f1}{0} {6:f2}{0} {7:f3}{0} {8:f2}{0} {9:f2}{0} {10:f2}{0} {11:f2}{0} {12:f2}{0} {13:f2}{0} {14}", separator,
                                      startSecond, startSecond + chunkDuration,
                                      rainIndices.AvSig_dB, rainIndices.BgNoise, rainIndices.Snr,
                                      rainIndices.Activity, rainIndices.Spikes, rainIndices.ACI,
                                      rainIndices.LowFreqCover, rainIndices.MidFreqCover, rainIndices.HiFreqCover,
                                      rainIndices.TemporalEntropy, rainIndices.SpectralEntropy, classification);

                //if (verbose)
                if (false)
                {
                    LoggedConsole.WriteLine(line);
                }

                //FOR PREPARING SEE.5 DATA  -------  write indices and clsasification info to file
                //sb.AppendLine(line);
            }

            //FOR PREPARING SEE.5 DATA   ------    write indices and clsasification info to file
            //string opDir = @"C:\SensorNetworks\Output\Rain";
            //string opPath = Path.Combine(opDir, recording.BaseName + ".Rain.csv");
            //FileTools.WriteTextFile(opPath, sb.ToString());

            Dictionary<string, double> dict = ConvertClassifcations2Dictionary(classifications);
            return dict;
        } //Analysis()

        /// <summary>
        /// returns some indices relevant to rain and cicadas from a short (10seconds) chunk of audio.
        /// </summary>
        /// <param name="signal">signal envelope of a 10s chunk of audio.</param>
        /// <param name="spectrogram">spectrogram of a 10s chunk of audio.</param>
        public static RainStruct Get10SecondIndices(double[] signal, double[,] spectrogram, int lowFreqBound, int midFreqBound,
                                                    TimeSpan frameDuration, double binWidth)
        {
            // i: FRAME ENERGIES -
            double StandardDeviationCount = 0.1;
            var results3 = SNR.SubtractBackgroundNoiseFromWaveform_dB(SNR.Signal2Decibels(signal), StandardDeviationCount); //use Lamel et al.
            var dBarray = SNR.TruncateNegativeValues2Zero(results3.NoiseReducedSignal);

            bool[] activeFrames = new bool[dBarray.Length]; //record frames with activity >= threshold dB above background and count
            for (int i = 0; i < dBarray.Length; i++)
            {
                if (dBarray[i] >= ActivityAndCover.DefaultActivityThresholdDb)
                {
                    activeFrames[i] = true;
                }
            }

            //int activeFrameCount = dBarray.Count((x) => (x >= AcousticIndices.DEFAULT_activityThreshold_dB));
            int activeFrameCount = DataTools.CountTrues(activeFrames);

            double spikeThreshold = 0.05;
            double spikeIndex = CalculateSpikeIndex(signal, spikeThreshold);

            //Console.WriteLine("spikeIndex=" + spikeIndex);
            //DataTools.writeBarGraph(signal);

            RainStruct rainIndices; // struct in which to store all indices
            rainIndices.Activity = activeFrameCount / (double)dBarray.Length;  //fraction of frames having acoustic activity
            rainIndices.BgNoise = results3.NoiseMode;                         //bg noise in dB
            rainIndices.Snr = results3.Snr;                               //snr
            rainIndices.AvSig_dB = 20 * Math.Log10(signal.Average());        //10 times log of amplitude squared
            rainIndices.TemporalEntropy = DataTools.EntropyNormalised(DataTools.SquareValues(signal)); //ENTROPY of ENERGY ENVELOPE
            rainIndices.Spikes = spikeIndex;

            // ii: calculate the bin id of boundary between mid and low frequency spectrum
            int lowBinBound = (int)Math.Ceiling(lowFreqBound / binWidth);
            var midbandSpectrogram = MatrixTools.Submatrix(spectrogram, 0, lowBinBound, spectrogram.GetLength(0) - 1, spectrogram.GetLength(1) - 1);

            // iii: ENTROPY OF AVERAGE SPECTRUM and VARIANCE SPECTRUM - at this point the spectrogram is still an amplitude spectrogram
            var tuple = SpectrogramTools.CalculateAvgSpectrumAndVarianceSpectrumFromAmplitudeSpectrogram(midbandSpectrogram);
            rainIndices.SpectralEntropy = DataTools.EntropyNormalised(tuple.Item1); //ENTROPY of spectral averages
            if (double.IsNaN(rainIndices.SpectralEntropy))
            {
                rainIndices.SpectralEntropy = 1.0;
            }

            // iv: CALCULATE Acoustic Complexity Index on the AMPLITUDE SPECTRUM
            var aciArray = AcousticComplexityIndex.CalculateAci(midbandSpectrogram);
            rainIndices.ACI = aciArray.Average();

            //v: remove background noise from the spectrogram
            double spectralBgThreshold = 0.015;      // SPECTRAL AMPLITUDE THRESHOLD for smoothing background

            //double[] modalValues = SNR.CalculateModalValues(spectrogram); //calculate modal value for each freq bin.
            //modalValues = DataTools.filterMovingAverage(modalValues, 7);  //smooth the modal profile
            //spectrogram = SNR.SubtractBgNoiseFromSpectrogramAndTruncate(spectrogram, modalValues);
            //spectrogram = SNR.RemoveNeighbourhoodBackgroundNoise(spectrogram, spectralBgThreshold);

            //vi: SPECTROGRAM ANALYSIS - SPECTRAL COVER. NOTE: spectrogram is still a noise reduced amplitude spectrogram
            SpectralActivity sa = ActivityAndCover.CalculateSpectralEvents(spectrogram, spectralBgThreshold, frameDuration, lowFreqBound, midFreqBound, binWidth);
            rainIndices.LowFreqCover = sa.LowFreqBandCover;
            rainIndices.MidFreqCover = sa.MidFreqBandCover;
            rainIndices.HiFreqCover = sa.HighFreqBandCover;

            //double[] coverSpectrum = sa.coverSpectrum;
            //double[] eventSpectrum = sa.eventSpectrum;

            return rainIndices;
        }

        public static double CalculateSpikeIndex(double[] envelope, double spikeThreshold)
        {
            int length = envelope.Length;

            // int isolatedSpikeCount = 0;
            double peakIntenisty = 0.0;
            double spikeIntensity = 0.0;

            var peaks = DataTools.GetPeaks(envelope);
            int peakCount = 0;
            for (int i = 1; i < length - 1; i++)
            {
                if (!peaks[i])
                {
                    continue; //count spikes
                }

                peakCount++;
                double diffMinus1 = Math.Abs(envelope[i] - envelope[i - 1]);
                double diffPlus1 = Math.Abs(envelope[i] - envelope[i + 1]);
                double avDifference = (diffMinus1 + diffPlus1) / 2;
                peakIntenisty += avDifference;
                if (avDifference > spikeThreshold)
                {
                    //isolatedSpikeCount++; // count isolated spikes
                    spikeIntensity += avDifference;
                }
            }

            if (peakCount == 0)
            {
                return 0.0;
            }

            return spikeIntensity / peakIntenisty;
        } //CalculateSpikeIndex()

        public static Dictionary<string, double> ConvertClassifcations2Dictionary(string[] classifications)
        {
            Dictionary<string, double> dict = new Dictionary<string, double>();

            int length = classifications.Length;
            int rainCount = 0;
            int cicadaCount = 0;
            for (int i = 0; i < length; i++)
            {
                if (classifications[i] == header_rain)
                {
                    rainCount++;
                }

                if (classifications[i] == header_cicada)
                {
                    cicadaCount++;
                }
            }

            dict.Add(header_rain, rainCount / (double)length);
            dict.Add(header_cicada, cicadaCount / (double)length);
            return dict;
        }

        /// <summary>
        /// The values in this class were derived from See5 runs of data taken from ????.
        /// </summary>
        public static string ConvertAcousticIndices2Classifcations(RainStruct indices)
        {
            string classification = header_negative;
            if (indices.Spikes > 0.2)
            {
                if (indices.HiFreqCover > 0.24)
                {
                    return header_rain;
                }
                else
                {
                    return header_negative;
                }
            }
            else
            {
                if (indices.SpectralEntropy < 0.6 && indices.BgNoise > -20)
                {
                    return header_cicada;
                }
            }

            return classification;
        }
    } // end Class
}