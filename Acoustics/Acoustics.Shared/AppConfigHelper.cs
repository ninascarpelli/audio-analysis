﻿namespace Acoustics.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;

    public static class AppConfigHelper
    {
        /// <summary>
        /// Gets FfmpegExe.
        /// </summary>
        public static string FfmpegExe
        {
            get
            {
                return GetString("AudioUtilityFfmpegExe");
            }
        }

        /// <summary>
        /// Gets FfmpegExe.
        /// </summary>
        public static string FfprobeExe
        {
            get
            {
                return GetString("AudioUtilityFfprobeExe");
            }
        }

        /// <summary>
        /// Gets WvunpackExe.
        /// </summary>
        public static string WvunpackExe
        {
            get
            {
                return GetString("AudioUtilityWvunpackExe");
            }
        }

        /// <summary>
        /// Gets Mp3SpltExe.
        /// </summary>
        public static string Mp3SpltExe
        {
            get
            {
                return GetString("AudioUtilityMp3SpltExe");
            }
        }

        /// <summary>
        /// Gets SoxExe.
        /// </summary>
        public static string SoxExe
        {
            get
            {
                return GetString("AudioUtilitySoxExe");
            }
        }

        /// <summary>
        /// Gets AnalysisProgramBaseDir.
        /// </summary>
        public static DirectoryInfo AnalysisProgramBaseDir
        {
            get
            {
                return GetDir("AnalysisProgramBaseDirectory", true);
            }
        }

        /// <summary>
        /// Gets AnalysisRunDir.
        /// </summary>
        public static DirectoryInfo AnalysisRunDir
        {
            get
            {
                return GetDir("AnalysisRunDirectory", true);
            }
        }

        /// <summary>
        /// Gets TargetSegmentSize.
        /// </summary>
        public static TimeSpan TargetSegmentSize
        {
            get
            {
                return TimeSpan.FromMilliseconds(GetDouble("TargetSegmentSizeMs"));
            }
        }

        /// <summary>
        /// Gets MinSegmentSize.
        /// </summary>
        public static TimeSpan MinSegmentSize
        {
            get
            {
                return TimeSpan.FromMilliseconds(GetDouble("MinSegmentSizeMs"));
            }
        }

        /// <summary>
        /// Gets OriginalAudioStorageDirs.
        /// </summary>
        public static IEnumerable<DirectoryInfo> OriginalAudioStorageDirs
        {
            get
            {
                CheckWebSiteRootPathSet();

                return GetDirs(WebsitePath, "OriginalAudioStorageDirs", true, ",");
            }
        }

        private static void CheckWebSiteRootPathSet()
        {
            if (string.IsNullOrEmpty(WebsitePath))
            {
                throw new InvalidOperationException("Absolute website path needs to be set before use");
            }
        }

        /// <summary>
        /// Gets SegmentedAudioStorageDirs.
        /// </summary>
        public static IEnumerable<DirectoryInfo> SegmentedAudioStorageDirs
        {
            get
            {
                CheckWebSiteRootPathSet();
                return GetDirs(WebsitePath, "SegmentedAudioStorageDirs", true, ",");
            }
        }

        /// <summary>
        /// Gets SpectrogramStorageDirs.
        /// </summary>
        public static IEnumerable<DirectoryInfo> SpectrogramStorageDirs
        {
            get
            {
                CheckWebSiteRootPathSet();
                return GetDirs(WebsitePath, "SpectrogramStorageDirs", true, ",");
            }
        }

        /// <summary>
        /// Gets LogDir.
        /// </summary>
        public static DirectoryInfo LogDir
        {
            get
            {
                return GetDir("LogDir", true);
            }
        }

        /// <summary>
        /// Gets UploadFolder.
        /// </summary>
        public static DirectoryInfo UploadFolder
        {
            get
            {
                return GetDir("UploadFolder", true);
            }
        }

        /// <summary>
        /// Gets the directory of the QutSensors.Shared.dll assembly.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">
        /// Could not get directory.
        /// </exception>
        /// <exception cref="Exception">
        /// Could not get directory.
        /// </exception>
        public static DirectoryInfo AssemblyDir
        {
            get
            {
                var assemblyDirString = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                if (!string.IsNullOrEmpty(assemblyDirString))
                {
                    var assemblyDir = new DirectoryInfo(assemblyDirString);

                    if (!Directory.Exists(assemblyDir.FullName))
                    {
                        throw new DirectoryNotFoundException("Could not find assembly directory: " + assemblyDir.FullName);
                    }

                    return assemblyDir;
                }

                throw new Exception("Cannot get assembly directory.");
            }
        }



        public static string WebsitePath { get; set; }

        public static string GetString(string key)
        {
            if (!ConfigurationManager.AppSettings.AllKeys.Any(k => k == key))
            {
                //throw new ConfigurationErrorsException("Could not find key: " + key);
                return string.Empty;
            }

            var value = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrEmpty(value))
            {
                throw new ConfigurationErrorsException("Found key, but it did not have a value: " + key);
            }

            return value;
        }

        public static bool Contains(string key)
        {
            return ConfigurationManager.AppSettings.AllKeys.Any(k => k == key);
        }

        public static IEnumerable<string> GetStrings(string key, params string[] separators)
        {
            var value = GetString(key);
            var values = value
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(v => !string.IsNullOrEmpty(v));

            if (!values.Any() || values.All(s => string.IsNullOrEmpty(s)))
            {
                throw new ConfigurationErrorsException("Key " + key + " exists but does not have a value");
            }

            return values;
        }

        public static bool GetBool(string key)
        {
            var value = GetString(key);

            bool valueParsed;
            if (bool.TryParse(value, out valueParsed))
            {
                return valueParsed;
            }

            throw new ConfigurationErrorsException(
                "Key " + key + " exists but could not be converted to a bool: " + value);
        }

        public static int GetInt(string key)
        {
            var value = GetString(key);

            int valueParsed;
            if (int.TryParse(value, out valueParsed))
            {
                return valueParsed;
            }

            throw new ConfigurationErrorsException(
                "Key " + key + " exists but could not be converted to a int: " + value);
        }

        public static double GetDouble(string key)
        {
            var value = GetString(key);

            double valueParsed;
            if (double.TryParse(value, out valueParsed))
            {
                return valueParsed;
            }

            throw new ConfigurationErrorsException(
                "Key " + key + " exists but could not be converted to a double: " + value);
        }

        public static DirectoryInfo GetDir(string key, bool checkExists)
        {
            var value = GetString(key);

            if (checkExists && !Directory.Exists(value))
            {
                throw new DirectoryNotFoundException("Could not find directory: " + key);
            }

            return new DirectoryInfo(value);
        }

        public static FileInfo GetFile(string key, bool checkExists)
        {
            var value = GetString(key);

            if (checkExists && !File.Exists(value))
            {
                throw new FileNotFoundException("Could not find file: " + key, value);
            }

            return new FileInfo(value);
        }

        public static IEnumerable<FileInfo> GetFiles(string key, bool checkExists, params string[] separators)
        {
            var value = GetString(key);
            var values = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var files = values.Where(v => !string.IsNullOrEmpty(v)).Select(v => new FileInfo(v));

            if (checkExists && files.Any(f => !File.Exists(f.FullName)))
            {
                var file = files.First(f => !File.Exists(f.FullName));
                throw new FileNotFoundException("Could not find a file for key: " + key, file.FullName);
            }

            return files;
        }

        public static long GetLong(string key)
        {
            var value = GetString(key);

            long valueParsed;
            if (long.TryParse(value, out valueParsed))
            {
                return valueParsed;
            }

            throw new ConfigurationErrorsException(
                "Key " + key + " exists but could not be converted to a long: " + value);
        }

        public static IEnumerable<DirectoryInfo> GetDirs(string webConfigRealDirectory,string key, bool checkExists, params string[] separators)
        {
            CheckWebSiteRootPathSet();

            var value = GetString(key);
            
            var values = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var dirs = values.Where(v => !string.IsNullOrEmpty(v)).Select(v => new DirectoryInfo(webConfigRealDirectory + v));

            if (checkExists && dirs.Any(d => !Directory.Exists(d.FullName)))
            {
                var dir = dirs.First(d => !Directory.Exists(d.FullName));
                throw new FileNotFoundException("Could not find a directory for key: " + key, dir.FullName);
            }

            return dirs;
        }
    }
}
