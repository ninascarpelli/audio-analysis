﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MiscDisplayManager.cs" company="MQUTeR">
//   -
// </copyright>
// <summary>
//   Misc Display methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace QutSensors.UI.Display.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using QutSensors.Data.Linq;
    using QutSensors.Shared;
    using QutSensors.UI.Display.Classes;

    /// <summary>
    /// Misc Display methods.
    /// </summary>
    public static class MiscDisplayManager
    {
        private const string FitKgbString = "http://fitkgbl07.fit.qut.edu.au";
        private const string DateFormatString = "yyyy-MM-dd HH:mm:ss";

        private const string AudioReadingHandlerString = "/AudioReading.ashx";

        private const string SpectrogramHandlerString = "/Spectrogram.ashx";

        private const string DoesNotExistString = "does not exist";

        private const string AudioPlayerXap = "/ClientBin/AudioPlayerControl.xap";

        private const string AxdFiles = ".axd?";

        private const string SqlServerTimeout =
            "The timeout period elapsed prior to completion of the operation or the server is not responding.";

        /// <summary>
        /// Get error log items.
        /// </summary>
        /// <param name="maxItems">
        /// The max items.
        /// </param>
        /// <param name="startIndex">
        /// The start index.
        /// </param>
        /// <param name="sortExpression">
        /// The sort expression.
        /// </param>
        /// <param name="displayFilter">
        /// The display Filter.
        /// </param>
        /// <returns>
        /// List of error log items.
        /// </returns>
        public static IEnumerable<ErrorLogItem> GetErrorLogItems(int maxItems, int startIndex, string sortExpression, string displayFilter)
        {
            using (var db = new QutSensorsDb())
            {
                var results = db.ErrorLogs.AsQueryable();

                switch (sortExpression)
                {
                    case "ErrorId":
                        results = results.OrderBy(s => s.ErrorID);
                        break;
                    case "ErrorId DESC":
                        results = results.OrderByDescending(s => s.ErrorID);
                        break;
                    case "Summary":
                        results = results.OrderBy(s => s.Error);
                        break;
                    case "Summary DESC":
                        results = results.OrderByDescending(s => s.Error);
                        break;
                    case "Detail":
                        results = results.OrderBy(s => s.Details);
                        break;
                    case "Detail DESC":
                        results = results.OrderByDescending(s => s.Details);
                        break;
                    case "Time":
                        results = results.OrderBy(s => s.Time);
                        break;
                    case "Time DESC":
                        results = results.OrderByDescending(s => s.Time);
                        break;
                    default:
                        results = results.OrderByDescending(s => s.ErrorID);
                        break;
                }

                results = ApplyDisplayFilter(results, displayFilter);

                return results.Skip(startIndex).Take(maxItems).Select(e => new ErrorLogItem
                            {
                                ErrorId = e.ErrorID,
                                Summary = HttpUtility.HtmlEncode(e.Error),
                                Detail = HttpUtility.HtmlEncode(e.Details),
                                Time = e.Time
                            }).ToList();
            }
        }

        /*
        -- access issues
select 
substring(Error,40,CHARINDEX(' ',Error, 40)-40) as AccessType, 
substring(Error,CHARINDEX(':',Error, 100)+2,100) as AccessUrl, 
MAX([time]) as LastRecordedDate,
COUNT(*) as AccessCount
from ErrorLogs
where 
 Error like '%AuthenticationHelper%'
 and Error not like '%http://fitkgbl07.fit.qut.edu.au/%'
group by 
substring(Error,40,CHARINDEX(' ',Error, 40)-40), 
substring(Error,CHARINDEX(':',Error, 100)+2,100),
datepart(year,[Time])
order by COUNT(*) desc
    */

        /// <summary>
        /// Get number of error log items.
        /// </summary>
        /// <returns>
        /// Number of error log items.
        /// </returns>
        public static int GetErrorLogItemsCount(string displayFilter)
        {
            using (var db = new QutSensorsDb())
            {
                return ApplyDisplayFilter(db.ErrorLogs.AsQueryable(), displayFilter).Count();
            }
        }

        private static IQueryable<ErrorLog> ApplyDisplayFilter(IQueryable<ErrorLog> results, string displayFilter)
        {
            // always filter out fitkgbl07 urls.
            results = results.Where(e => !e.Details.Contains(FitKgbString));

            if (!string.IsNullOrEmpty(displayFilter))
            {
                switch (displayFilter)
                {
                    case "AudioReadingHandler":
                        results = results.Where(e => e.Error.Contains(AudioReadingHandlerString));
                        break;
                    case "SpectrogramHandler":
                        results = results.Where(e => e.Error.Contains(SpectrogramHandlerString));
                        break;
                    case "MissingFiles":
                        results = results.Where(e => e.Error.Contains(DoesNotExistString));
                        break;
                    case "AudioPlayer":
                        results = results.Where(e => e.Error.Contains(AudioPlayerXap));
                        break;
                    case "ScriptFiles":
                        results = results.Where(e => e.Error.Contains(AxdFiles));
                        break;
                    case "Timeout":
                        results = results.Where(e => e.Error.Contains(SqlServerTimeout));
                        break;
                }
            }

            return results;
        }

        /// <summary>
        /// Get data items paged.
        /// </summary>
        /// <param name="maxItems">
        /// The max items.
        /// </param>
        /// <param name="startIndex">
        /// The start index.
        /// </param>
        /// <param name="sortExpression">
        /// The sort expression.
        /// </param>
        /// <returns>
        /// List of data items.
        /// </returns>
        public static IEnumerable<DataCacheItem> GetDataCacheItems(int maxItems, int startIndex, string sortExpression)
        {
            using (var db = new QutSensorsDb())
            {
                var results = db.Cache_JobItems.AsQueryable();

                switch (sortExpression)
                {
                    case "JobId":
                        results = results.OrderBy(s => s.JobID);
                        break;
                    case "JobId DESC":
                        results = results.OrderByDescending(s => s.JobID);
                        break;
                    case "JobItemId":
                        results = results.OrderBy(s => s.JobItemID);
                        break;
                    case "JobItemId DESC":
                        results = results.OrderByDescending(s => s.JobItemID);
                        break;
                    case "Type":
                        results = results.OrderBy(s => s.Type);
                        break;
                    case "Type DESC":
                        results = results.OrderByDescending(s => s.Type);
                        break;
                    case "Status":
                        results = results.OrderBy(s => s.Status);
                        break;
                    case "Status DESC":
                        results = results.OrderByDescending(s => s.Status);
                        break;
                    case "LastAccessed":
                        results = results.OrderBy(s => s.Data.LastAccessed);
                        break;
                    case "LastAccessed DESC":
                        results = results.OrderByDescending(s => s.Data.LastAccessed);
                        break;
                    case "CreatedTime":
                        results = results.OrderBy(s => s.Data.CreatedTime);
                        break;
                    case "CreatedTime DESC":
                        results = results.OrderByDescending(s => s.Data.CreatedTime);
                        break;
                    case "TimeSpentGenerating":
                        results = results.OrderBy(s => s.Data.TimeSpentGenerating);
                        break;
                    case "TimeSpentGenerating DESC":
                        results = results.OrderByDescending(s => s.Data.TimeSpentGenerating);
                        break;
                    case "ProcessingStartTime":
                        results = results.OrderBy(s => s.ProcessingStartTime);
                        break;
                    case "ProcessingStartTime DESC":
                        results = results.OrderByDescending(s => s.ProcessingStartTime);
                        break;
                    case "Start":
                        results = results.OrderBy(s => s.Start);
                        break;
                    case "Start DESC":
                        results = results.OrderByDescending(s => s.Start);
                        break;
                    case "End":
                        results = results.OrderBy(s => s.End);
                        break;
                    case "End DESC":
                        results = results.OrderByDescending(s => s.End);
                        break;
                    case "MimeType":
                        results = results.OrderBy(s => s.MimeType);
                        break;
                    case "MimeType DESC":
                        results = results.OrderByDescending(s => s.MimeType);
                        break;
                    case "Duration":
                        results = results.OrderBy(s => s.End - s.Start);
                        break;
                    case "Duration DESC":
                        results = results.OrderByDescending(s => s.End - s.Start);
                        break;
                    default:
                        results = results.OrderByDescending(s => s.Data.LastAccessed);
                        break;
                }

                var items = results.Skip(startIndex).Take(maxItems);

                var count = items.Count();
                if (count < 1)
                {
                    return new List<DataCacheItem>();
                }

                var show = results.Skip(startIndex).Take(maxItems).Select(s =>
                    new
                        {
                            JobId = s.JobID,
                            JobItemId = s.JobItemID,
                            Type = s.Type,
                            Status = s.Status,
                            MimeType = s.MimeType,
                            ProcessingStartTime = s.ProcessingStartTime,
                            LastAccessed = s.Status == CacheJobItemStatus.Complete ? s.Data.LastAccessed : new DateTime?(),
                            TimeSpentGeneratingMs = s.Status == CacheJobItemStatus.Complete ? s.Data.TimeSpentGenerating.TotalMilliseconds : 0,
                            CreatedTime = s.Status == CacheJobItemStatus.Complete ? s.Data.CreatedTime : new DateTime?(),
                            Time = s.Job.AudioReading.Time,
                            End = s.End,
                            Start = s.Start,
                            AudioReadingId = s.Job.AudioReadingID
                        });


                var display = show.ToList().Select(s => new DataCacheItem
            {
                JobId = s.JobId,
                JobItemId = s.JobItemId,
                Type = s.Type.ToString(),
                Status = s.Status,
                MimeType = s.MimeType,
                ProcessingStartTime = s.ProcessingStartTime.HasValue ? s.ProcessingStartTime.Value.ToString(DateFormatString) : "not started",
                LastAccessed = s.LastAccessed.HasValue ? s.LastAccessed.Value.ToString(DateFormatString) : "unknown",
                TimeSpentGenerating = s.TimeSpentGeneratingMs != 0 ? new TimeSpan((long)s.TimeSpentGeneratingMs * TimeSpan.TicksPerMillisecond).ToReadableString() : "unknown",
                CreatedTime = s.CreatedTime.HasValue ? s.CreatedTime.Value.ToString(DateFormatString) : "unknown",
                End = s.Time.AddMilliseconds(s.End).ToString(DateFormatString),
                Start = s.Time.AddMilliseconds(s.Start).ToString(DateFormatString),
                Duration = (s.Time.AddMilliseconds(s.Start) - s.Time.AddMilliseconds(s.End)).ToReadableString(),
                AudioReadingIdQs = new ReadingsFilter { CommaSeparatedAudioReadingIds = s.AudioReadingId.ToString() }.ToQueryString(true)
            });

                return display;
            }
        }

        /// <summary>
        /// Get number of data cache items.
        /// </summary>
        /// <returns>
        /// Number of cache items.
        /// </returns>
        public static int GetDataCacheItemsCount()
        {
            using (var db = new QutSensorsDb())
            {
                return db.Cache_JobItems.Count();
            }
        }
    }
}