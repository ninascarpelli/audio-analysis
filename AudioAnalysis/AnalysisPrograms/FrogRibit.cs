﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TowseyLib;
using AudioAnalysisTools;


namespace AnalysisPrograms
{
    class FrogRibit
    {

        public static void Dev(string[] args)
        {
            string title = "# DETECT FROG RIBBIT.";
            string date  = "# DATE AND TIME: " + DateTime.Now;
            Log.WriteLine(title);
            Log.WriteLine(date);

            //SET VERBOSITY
            Log.Verbosity = 1;

            //string recordingPath   = args[0];
            //string iniPath        = args[0];
            //string targetName     = args[2];   //prefix of name of created files 

            string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\DataSet\Rheobatrachus_silus_MONO.wav";
            double windowDuration = 5.0; // milliseconds - NOTE: 128 samples @ 22.050kHz = 5.805ms.
            int midFreq = 1550; // middle of freq band of interest 


            //i: Set up the file names
            //string outputDir = Path.GetDirectoryName(iniPath) + "\\";

            //ii: READ PARAMETER VALUES FROM INI FILE
            //var config = new Configuration(iniPath);
            //Dictionary<string, string> dict = config.GetTable();
            //string sourceFile = dict[FeltTemplate_Create.key_SOURCE_RECORDING];
            //string sourceDir = dict[FeltTemplate_Create.key_SOURCE_DIRECTORY];
            //double dB_Threshold = Double.Parse(dict[FeltTemplate_Create.key_DECIBEL_THRESHOLD]);
            //double maxTemplateIntensity = Double.Parse(dict[FeltTemplate_Create.key_TEMPLATE_MAX_INTENSITY]);
            //int neighbourhood = Int32.Parse(dict[FeltTemplate_Create.key_DONT_CARE_NH]);   //the do not care neighbourhood
            //int lineLength = Int32.Parse(dict[FeltTemplate_Create.key_LINE_LENGTH]);
            //double templateThreshold = dB_Threshold / maxTemplateIntensity;
            //int bitmapThreshold = (int)(255 - (templateThreshold * 255));

            //i: GET RECORDING
            AudioRecording recording = new AudioRecording(recordingPath);
            //if (recording.SampleRate != 22050) recording.ConvertSampleRate22kHz();
            int sr = recording.SampleRate;

            string filterName = "Chebyshev_Lowpass_3000";
            System.Console.WriteLine("\nApply filter: " + filterName);
            var filteredRecording = recording.Filter_IIR(filterName); //return new filtered audio recording.
            //recording.Dispose(); //DO NOT DISPOSE BECAUSE REQUIRE AGAIN

            //ii: SET UP CONFIGURATION
            Log.WriteLine("Start sonogram.");
            SonogramConfig sonoConfig = new SonogramConfig(); //default values config
            sonoConfig.SourceFName = recording.FileName;
            sonoConfig.WindowSize = (int)(windowDuration * sr / 1000.0);
            sonoConfig.WindowOverlap = 0.5;      // set default value
            sonoConfig.DoMelScale = false;
            sonoConfig.NoiseReductionType = NoiseReductionType.NONE;
            //sonoConfig.NoiseReductionType = NoiseReductionType.STANDARD;
            int signalLength = filteredRecording.GetWavReader().Samples.Length;

            //iii: FRAMING
            int[,] frameIDs = DSP_Frames.FrameStartEnds(signalLength, sonoConfig.WindowSize, sonoConfig.WindowOverlap);
            int frameCount = frameIDs.GetLength(0);

            //iv: EXTRACT ENVELOPE and ZERO-CROSSINGS
            var results2 = DSP_Frames.ExtractEnvelopeAndZeroCrossings(filteredRecording.GetWavReader().Samples, sr, sonoConfig.WindowSize, sonoConfig.WindowOverlap);
            double[] average       = results2.Item1;
            double[] envelope      = results2.Item2;
            double[] zeroCrossings = results2.Item3;
            double[] sampleZCs     = results2.Item4;
            double[] sampleStd     = results2.Item5;


            //v: FRAME ENERGIES
            var results3 = SNR.SubtractBackgroundNoise(SNR.Signal2Decibels(envelope));
            //var results3 = SNR.SubtractBackgroundNoise(SNR.Signal2Decibels(average));
            var dBarray3 = SNR.TruncateNegativeValues2Zero(results3.Item1);
            //AUDIO SEGMENTATION
            //SigState = EndpointDetectionConfiguration.DetermineVocalisationEndpoints(DecibelsPerFrame, this.FrameOffset);

            //vi: CONVERSIONS: ZERO CROSSINGS to herz; samples to std dev
            int[] freq = DSP_Frames.ConvertZeroCrossings2Hz(zeroCrossings, sonoConfig.WindowSize, sr);
            // convert sample std deviations to milliseconds
            double[] tsd = DSP_Frames.ConvertSamples2Milliseconds(sampleStd, sr); //time standard deviation
            //filter the freq array to remove values derived from frames with high standard deviation
            double[] filteredArray = FilterFreqArray(freq, tsd, midFreq);



            //vii: MAKE SONOGRAM
            sonoConfig.WindowSize = SonogramConfig.DEFAULT_WINDOW_SIZE/2;

            //AmplitudeSonogram basegram = new AmplitudeSonogram(sonoConfig, recording.GetWavReader());
            AmplitudeSonogram basegram = new AmplitudeSonogram(sonoConfig, filteredRecording.GetWavReader());
            SpectralSonogram  sonogram = new SpectralSonogram(basegram);         //spectrogram has dim[N,257]

            //write the signal: IMPORTANT: ENSURE VALUES ARE IN RANGE -32768 to +32768
            //int bitRate = 16;
            //WavWriter.WriteWavFile(filteredRecording.GetWavReader().Samples, filteredRecording.SampleRate, bitRate, recordingPath + "filtered.wav");        

            string imagePath = recordingPath + ".png";
            DrawSonogram(sonogram, imagePath, dBarray3, filteredArray, tsd);

            Log.WriteLine("# Finished everything!");
            Console.ReadLine();  
        } //DEV()


        public static double[] FilterFreqArray(int[] freq, double[] tsd, int midFreq)
        {
            //get av and std of the background time variation
            double avBG = 0.0;
            double sdBG = 0.0;
            NormalDist.AverageAndSD(tsd, out avBG, out sdBG);
            //calculate a threshold using 3 standard deviations;
            double threshold = avBG - (1.2 * sdBG);

            int L = freq.Length;
            var filteredArray = new double[L];
            for (int i = 0; i < L; i++)
            {
                int freqGap = Math.Abs(midFreq - freq[i]);
                if ((freqGap<300) && (tsd[i] < threshold)) filteredArray[i] = freq[i];
            }
            return filteredArray;
        }

        public static void DrawSonogram(BaseSonogram sonogram, string path, double[] decibelArray, double[] av, double[] sd)
        {
            Log.WriteLine("# Start to draw image of sonogram.");
            bool doHighlightSubband = false; bool add1kHzLines = true;
            //sonogram.FramesPerSecond = 1 / sonogram.FrameOffset;


            int dbMaxIndex = DataTools.GetMaxIndex(decibelArray);
            int avMaxIndex = DataTools.GetMaxIndex(av);
            int sdMaxIndex = DataTools.GetMaxIndex(sd);

            using (System.Drawing.Image img = sonogram.GetImage(doHighlightSubband, add1kHzLines))
            using (Image_MultiTrack image = new Image_MultiTrack(img))
            {
                //img.Save(@"C:\SensorNetworks\WavFiles\temp1\testimage1.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                image.AddTrack(Image_Track.GetTimeTrack(sonogram.Duration, sonogram.FramesPerSecond));
                image.AddTrack(Image_Track.GetScoreTrack(decibelArray, 0, decibelArray[dbMaxIndex], 5));
                image.AddTrack(Image_Track.GetScoreTrack(av, 0.0, av[avMaxIndex], 1.0));
                image.AddTrack(Image_Track.GetScoreTrack(sd, 0.0, sd[sdMaxIndex], 1.0));
                image.Save(path);
            } // using
        } // DrawSonogram()

    }
}
 