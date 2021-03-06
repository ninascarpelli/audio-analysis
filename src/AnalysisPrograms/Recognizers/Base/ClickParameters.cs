// <copyright file="ClickParameters.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AnalysisPrograms.Recognizers.Base
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Acoustics.Shared;
    using AudioAnalysisTools;
    using AudioAnalysisTools.StandardSpectrograms;

    /// <summary>
    /// Parameters needed from a config file to detect click components.
    /// </summary>
    [YamlTypeTag(typeof(ClickParameters))]
    public class ClickParameters : CommonParameters
    {
        /// <summary>
        /// Gets or sets the minimum bandwidth, units = Hertz.
        /// </summary>
        public int? MinBandwidthHertz { get; set; }

        /// <summary>
        /// Gets or sets maximum bandwidth, units = Hertz.
        /// </summary>
        public int? MaxBandwidthHertz { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether proximal similar clicks are to be combined.
        /// Proximal means the clicks' time starts are not separated by more than the specified seconds interval.
        /// Similar means that the clicks' frequency bounds do not differ by more than the specified Hertz interval.
        /// </summary>
        public bool CombineProximalSimilarEvents { get; set; }

        public TimeSpan StartDifference { get; set; }

        public int HertzDifference { get; set; }

        /// <summary>
        /// A click is a sharp onset broadband sound of brief duration. Geometrically it is similar to a vertical whistle.
        /// THis method averages dB log values incorrectly but it is faster than doing many log conversions.
        /// This method is used to find acoustic events and is accurate enough for the purpose.
        /// </summary>
        public static (List<AcousticEvent> Events, double[] Intensity) GetClicks(
            SpectrogramStandard sonogram,
            int minHz,
            int maxHz,
            int nyquist,
            double decibelThreshold,
            int minBandwidthHertz,
            int maxBandwidthHertz,
            bool combineProximalSimilarEvents,
            TimeSpan segmentStartOffset)
        {
            var sonogramData = sonogram.Data;
            int frameCount = sonogramData.GetLength(0);
            int binCount = sonogramData.GetLength(1);

            double binWidth = nyquist / (double)binCount;
            int minBin = (int)Math.Round(minHz / binWidth);
            int maxBin = (int)Math.Round(maxHz / binWidth);
            int bandwidthBinCount = maxBin - minBin + 1;

            // list of accumulated acoustic events
            var events = new List<AcousticEvent>();
            var temporalIntensityArray = new double[frameCount];

            // for all time frames except 1st and last allowing for edge effects.
            for (int t = 1; t < frameCount - 1; t++)
            {
                // set up an intensity array for all frequency bins in this frame.
                double[] clickIntensity = new double[bandwidthBinCount];

                // buffer zone around click is one frame wide.
                // for all frequency bins except top and bottom in this time frame
                for (int bin = minBin; bin < maxBin; bin++)
                {
                    // THis is where the profile of a click is defined
                    // A click requires sudden onset, with maximum amplitude followed by decay.
                    if (sonogramData[t - 1, bin] > decibelThreshold || sonogramData[t, bin] < sonogramData[t + 1, bin])
                    {
                        continue;
                    }

                    // THis is where the profile of a vertical ridge is defined
                    //if (sonogramData[t, bin] < sonogramData[t - 1, bin] || sonogramData[t, bin] < sonogramData[t + 1, bin])
                    //{
                    //    continue;
                    //}

                    clickIntensity[bin - minBin] = sonogramData[t, bin];
                    //clickIntensity[bin - minBin] = sonogramData[t, bin] - sonogramData[t - 1, bin];
                    clickIntensity[bin - minBin] = Math.Max(0.0, clickIntensity[bin - minBin]);
                }

                if (clickIntensity.Max() < decibelThreshold)
                {
                    continue;
                }

                // Extract the events based on bandwidth and threshhold.
                var acousticEvents = ConvertSpectralArrayToClickEvents(
                    clickIntensity,
                    minHz,
                    sonogram.FramesPerSecond,
                    sonogram.FBinWidth,
                    decibelThreshold,
                    minBandwidthHertz,
                    maxBandwidthHertz,
                    t,
                    segmentStartOffset);

                // add each event score to combined temporal intensity array
                foreach (var ae in acousticEvents)
                {
                    var avClickIntensity = ae.Score;
                    temporalIntensityArray[t] += avClickIntensity;
                }

                // add new events to list of events
                events.AddRange(acousticEvents);
            }

            // combine proximal events that occupy similar frequency band
            var startDifference = TimeSpan.FromSeconds(1.0);
            var hertzDifference = 100;
            if (combineProximalSimilarEvents)
            {
                events = AcousticEvent.CombineSimilarProximalEvents(events, startDifference, hertzDifference);
            }

            return (events, temporalIntensityArray);
        }

        /// <summary>
        /// A general method to convert an array of score values to a list of AcousticEvents.
        /// NOTE: The score array is assumed to be a spectrum of dB intensity.
        /// The method uses the passed scoreThreshold in order to calculate a normalised score.
        /// Max possible score := threshold * 5.
        /// normalised score := score / maxPossibleScore.
        /// Some analysis techniques (e.g. Oscillation Detection) have their own methods for extracting events from score arrays.
        /// </summary>
        /// <param name="clickIntensityArray">the array of click intensity.</param>
        /// <param name="minHz">lower freq bound of the search band for click events.</param>
        /// <param name="framesPerSec">the time scale required by AcousticEvent class.</param>
        /// <param name="freqBinWidth">the freq scale required by AcousticEvent class.</param>
        /// <param name="scoreThreshold">threshold for the intensity values.</param>
        /// <param name="minBandwidth">bandwidth of click must exceed this to qualify as an event.</param>
        /// <param name="maxBandwidth">bandwidth of click must be less than this to qualify as an event.</param>
        /// <param name="frameNumber">time of start of the current frame.</param>
        /// <returns>a list of acoustic events.</returns>
        public static List<AcousticEvent> ConvertSpectralArrayToClickEvents(
            double[] clickIntensityArray,
            int minHz,
            double framesPerSec,
            double freqBinWidth,
            double scoreThreshold,
            int minBandwidth,
            int maxBandwidth,
            int frameNumber,
            TimeSpan segmentStartOffset)
        {
            int binCount = clickIntensityArray.Length;
            var events = new List<AcousticEvent>();
            double maxPossibleScore = 5 * scoreThreshold; // used to calculate a normalised score between 0 - 1.0
            bool isHit = false;
            double frameOffset = 1 / framesPerSec;
            int bottomFrequency = minHz; // units = Hertz
            int bottomBin = 0;

            // pass over all frequency bins except last two due to edge effect later.
            for (int i = 0; i < binCount - 2; i++)
            {
                if (isHit == false && clickIntensityArray[i] >= scoreThreshold)
                {
                    //low freq end of a click event
                    isHit = true;
                    bottomBin = i;
                    bottomFrequency = minHz + (int)Math.Round(i * freqBinWidth);
                }
                else // check for the high frequency end of a click event
                if (isHit && clickIntensityArray[i] <= scoreThreshold)
                {
                    // now check if there is acoustic intensity in next two frequncy bins
                    double avIntensity = (clickIntensityArray[i] + clickIntensityArray[i + 1] + clickIntensityArray[i + 2]) / 3;

                    if (avIntensity >= scoreThreshold)
                    {
                        // this is not top of click - it continues through to higher frequency bins.
                        continue;
                    }

                    // bin (i - 1) is the upper Hz end of an event, so initialise it
                    isHit = false;
                    double eventBinWidth = i - bottomBin;
                    double hzBandwidth = (int)Math.Round(eventBinWidth * freqBinWidth);

                    //skip events having wrong bandwidth
                    if (hzBandwidth < minBandwidth || hzBandwidth > maxBandwidth)
                    {
                        continue;
                    }

                    // obtain an average score for the bandwidth of the potential event.
                    double av = 0.0;
                    for (int n = bottomBin; n <= i; n++)
                    {
                        av += clickIntensityArray[n];
                    }

                    av /= eventBinWidth;

                    // Initialize the event with: TimeSpan segmentStartOffset, double eventStartSegmentRelative, double eventDuration, etc
                    // Click events are assumed to be two frames duration.
                    double eventDuration = frameOffset * 2;
                    double startTimeRelativeSegment = frameOffset * frameNumber;
                    var ev = new AcousticEvent(segmentStartOffset, startTimeRelativeSegment, eventDuration, bottomFrequency, bottomFrequency + hzBandwidth);
                    ev.SetTimeAndFreqScales(frameOffset, freqBinWidth);
                    ev.Score = av;

                    // normalised to the user supplied threshold
                    ev.ScoreNormalised = ev.Score / maxPossibleScore;
                    if (ev.ScoreNormalised > 1.0)
                    {
                        ev.ScoreNormalised = 1.0;
                    }

                    ev.Score_MaxPossible = maxPossibleScore;

                    //find max score
                    double max = -double.MaxValue;
                    for (int n = bottomBin; n <= i; n++)
                    {
                        if (clickIntensityArray[n] > max)
                        {
                            max = clickIntensityArray[n];
                            ev.Score_MaxInEvent = clickIntensityArray[n];
                        }
                    }

                    events.Add(ev);
                }
            }

            return events;
        }
    }
}
