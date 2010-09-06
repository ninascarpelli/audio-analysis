﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheJobProcessor.cs" company="MQUTeR">
//   -
// </copyright>
// <summary>
//   Defines the CacheJobProcessor type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace QutSensors.CacheProcessor
{
    using System;
    using System.Diagnostics;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using AudioTools;

    using QutSensors.Business.Audio;
    using QutSensors.Business.Cache;
    using QutSensors.Data;
    using QutSensors.Data.Linq;
    using QutSensors.Shared;
    using QutSensors.Shared.LogProviders;
    using System.Configuration;

    /// <summary>
    /// Cache Processor.
    /// </summary>
    public class CacheJobProcessor
    {
        /// <summary>
        /// Time to wait between checks for jobs in milliseconds.
        /// </summary>
        private const int InterJobWaitPeriod = 5000;

        private readonly ManualResetEvent stopRequestedEvent = new ManualResetEvent(false);
        private readonly ILogProvider log;
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// Worker thread for generating cache data.
        /// </summary>
        private Thread workerThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheJobProcessor"/> class.
        /// </summary>
        /// <param name="log">
        /// Log provider to use.
        /// </param>
        /// <param name="cacheManager">
        /// The cache manager.
        /// </param>
        public CacheJobProcessor(ILogProvider log, ICacheManager cacheManager)
        {
            this.log = log;
            this.cacheManager = cacheManager;
        }

        /// <summary>
        /// Gets a value indicating whether IsRunning.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return stopRequestedEvent.WaitOne(0);
            }
        }

        /// <summary>
        /// Start the processor.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Start()
        {
            if (workerThread != null)
            {
                throw new InvalidOperationException("Worker thread already started.");
            }

            workerThread = new Thread(ThreadMain);
            workerThread.Start();
        }

        /// <summary>
        /// Stop the processor.
        /// </summary>
        public void Stop()
        {
            stopRequestedEvent.Set();
        }

        /// <summary>
        /// Get audio reading from db.
        /// </summary>
        /// <param name="jobId">
        /// The job Id.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <c>InvalidOperationException</c>.
        /// </exception>
        /// <returns>
        /// Audio Reading.
        /// </returns>
        private static AudioReading GetAudioReading(int jobId)
        {
            using (var db = new QutSensorsDb())
            {
                // get audio file, and save to file.
                var reading = db.Cache_Jobs.Where(j => j.JobID == jobId).Select(j => j.AudioReading).FirstOrDefault();

                return reading;
            }
        }

        /// <summary>
        /// Save AudioReading to file.
        /// </summary>
        /// <param name="audioReading">AudioReading to save.</param>
        /// <returns>Path to audio file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="audioReading" /> is <c>null</c>.</exception>
        private static TempFile SaveAudioReading(AudioReading audioReading)
        {
            if (audioReading == null)
            {
                throw new ArgumentNullException("audioReading");
            }

            var sourceFile = new TempFile(MimeTypes.GetExtension(audioReading.MimeType));

            using (var sqlFile = SqlFilestream.CreateAudioReading(QutSensorsDb.ConnectionString, audioReading.AudioReadingID))
            using (var fileStream = new System.Data.SqlTypes.SqlFileStream(sqlFile.FileName, sqlFile.Context, FileAccess.Read))
            {
                sourceFile.CopyStream(fileStream);
            }

            return sourceFile;
        }

        /// <summary>
        /// Main method for thread.
        /// </summary>
        private void ThreadMain()
        {
            try
            {
                if (log != null)
                {
                    log.WriteEntry(LogType.Information, "Cache Job Processor starting");
                }

                do
                {
                    if (!ProcessJob())
                    {
                        // No job or error so wait for new job to process.
                        stopRequestedEvent.WaitOne(InterJobWaitPeriod);
                    }
                }
                while (!stopRequestedEvent.WaitOne(InterJobWaitPeriod));

                if (log != null)
                {
                    log.WriteEntry(LogType.Information, "Cache Job Processor stopping");
                }
            }
            catch (Exception ex)
            {
                if (log != null)
                {
                    log.WriteEntry(LogType.Error, ex.ToString());
                }
            }
            finally
            {
                stopRequestedEvent.Reset();
                workerThread = null;
            }
        }

        /// <summary>
        /// The process job.
        /// </summary>
        /// <exception cref="NotImplementedException">
        /// Unable to generate spectrogram from job processor
        /// </exception>
        /// <returns>
        /// True if job was processed successfully, otherwise false.
        /// </returns>
        /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
        /// <exception cref="DirectoryNotFoundException"><c>DirectoryNotFoundException</c>.</exception>
        private bool ProcessJob()
        {
            var jobId = cacheManager.GetUnprocessedJob();
            if (jobId.HasValue)
            {
                try
                {
                    var reading = GetAudioReading(jobId.Value);

                    if (reading == null)
                    {
                        throw new InvalidOperationException("Could not get audio reading for job.");
                    }

                    using (var tempFile = SaveAudioReading(reading))
                    {
                        while (ProcessJobItem(tempFile.FileName, reading.Length, jobId.Value))
                        {
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    if (log != null)
                    {
                        log.WriteEntry(LogType.Error, "Error processing job id {0}: {1}", jobId.Value, e);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Process a job item.
        /// </summary>
        /// <param name="audioFile">
        /// The audio File.
        /// </param>
        /// <param name="audioFileDurationMs">
        /// The audio File Duration Ms.
        /// </param>
        /// <param name="jobId">
        /// The job Id.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <c>InvalidOperationException</c>.
        /// </exception>
        /// <returns>
        /// true if the item was processed successfully, otherwise false.
        /// </returns>
        private bool ProcessJobItem(string audioFile, long? audioFileDurationMs, int jobId)
        {
            var request = cacheManager.GetUnprocessedRequest(jobId);
            if (request != null)
            {
                try
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    byte[] data;
                    switch (request.Type)
                    {
                        case CacheJobType.AudioSegmentation:
                            data = this.SegmentAudio(audioFile, audioFileDurationMs, request);
                            break;
                        case CacheJobType.SpectrogramGeneration:
                            data = this.GenerateSpectrogram(audioFile, audioFileDurationMs, request);
                            break;
                        default:
                            throw new InvalidOperationException("Unrecognised CacheRequest type: " + request.Type);
                    }

                    stopWatch.Stop();

                    if (data != null)
                    {
                        cacheManager.Insert(request, data, stopWatch.Elapsed);
                    }

                    return true;
                }
                catch (Exception e)
                {
                    if (log != null)
                    {
                        log.WriteEntry(LogType.Error, "Error processing item: {0}", e);
                    }

                    cacheManager.SubmitError(request, e.ToString());
                }
            }

            return false;
        }

        /// <summary>
        /// The segment audio.
        /// </summary>
        /// <param name="audioFile">
        /// The audio File.
        /// </param>
        /// <param name="audioFileDurationMs">
        /// The audio File Duration Ms.
        /// </param>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Unable to find referenced AudioReading
        /// </exception>
        /// <returns>
        /// Byte array representing segment of audio.
        /// </returns>
        private byte[] SegmentAudio(string audioFile, long? audioFileDurationMs, CacheRequest request)
        {
            var transformer = new AudioTransformer(audioFile);

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                transformer.Segment(
                    request.Start,
                    audioFileDurationMs == request.End ? null : request.End,
                    request.MimeType,
                    ms);

                bytes = ms.GetBuffer();
            }

            if (log != null)
            {
                log.WriteEntry(
                    LogType.Information,
                    "Segmented audio {0} ({1}-{2})",
                    request.AudioReadingID,
                    request.Start,
                    request.End);
            }

            return bytes;
        }

        /// <summary>
        /// Generate spectrogram based on CacheRequest.
        /// </summary>
        /// <param name="audioFile">
        /// The audio File.
        /// </param>
        /// <param name="audioFileDurationMs">
        /// The audio File Duration Ms.
        /// </param>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Unable to find referenced AudioReading
        /// </exception>
        /// <returns>
        /// The generated spectrogram.
        /// </returns>
        private byte[] GenerateSpectrogram(string audioFile, long? audioFileDurationMs, CacheRequest request)
        {
            var transformer = new AudioTransformer(audioFile);

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                var image = transformer.GenerateSpectrogram(
                    request.Start,
                    audioFileDurationMs == request.End ? null : request.End);

                image.Save(ms, ImageFormat.Jpeg);
                bytes = ms.GetBuffer();
            }

            if (log != null)
            {
                log.WriteEntry(
                    LogType.Information,
                    "Generated spectrogram {0} ({1}-{2})",
                    request.AudioReadingID,
                    request.Start,
                    request.End);
            }

            return bytes;
        }
    }
}