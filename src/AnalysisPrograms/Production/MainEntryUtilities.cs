﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainEntryUtilities.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Defines the MainEntry type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

// ReSharper disable once CheckNamespace
namespace AnalysisPrograms
{
    using Acoustics.Shared.Contracts;

        #if DEBUG
#endif
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Acoustics.Shared;
    using log4net.Appender;
    using log4net.Core;
    using log4net.Repository.Hierarchy;
#if DEBUG
    using Acoustics.Shared.Debugging;
#endif
    using log4net;
    using McMaster.Extensions.CommandLineUtils;
    using Production;
    using Production.Arguments;
    using Production.Parsers;

    public static partial class MainEntry
    {
        // ReSharper disable once InconsistentNaming
#if DEBUG
        public const bool InDEBUG = true;
#else
        public const bool InDEBUG = false;
#endif

        public static readonly Dictionary<string, string> EnvironmentOptions =
            new Dictionary<string, string>
            {
                {
                    ApPlainLoggingKey,
                    "<true|false>\t Enable simpler logging - the default value is `false`"
                },
                {
                    ApMetricsKey,
                    "<true|false>\t (Not implemented) Enable or disable metrics - default value is `true`"
                },
            };

        private const string ApPlainLoggingKey = "AP_PLAIN_LOGGING";
        private const string ApMetricsKey = "AP_METRICS";

        internal enum Usages
        {
            All,
            Single,
            ListAvailable,
            NoAction,
        }

        /// <summary>
        /// Gets a value indicating whether or not we should use simpler logging semantics. Usually means no color.
        /// </summary>
        public static bool ApPlainLogging { get; private set; }

        /// <summary>
        /// Gets a value indicating whether we will submit metrics to the remote metric server.
        /// </summary>
        public static bool ApMetricRecording { get; private set; }

        public static CommandLineApplication CommandLineApplication { get; private set; }

        public static bool IsDebuggerAttached => Debugger.IsAttached;

        public static void SetLogVerbosity(LogVerbosity logVerbosity, bool quietConsole = false)
        {
            Level modifiedLevel;
            switch (logVerbosity)
            {
                case LogVerbosity.None:
                    // we never turn the logger completely off - sometimes the logger just really needs to log something.
                    modifiedLevel = LogExtensions.PromptLevel;
                    break;
                case LogVerbosity.Error:
                    modifiedLevel = Level.Error;
                    break;
                case LogVerbosity.Warn:
                    modifiedLevel = Level.Warn;
                    break;
                case LogVerbosity.Info:
                    modifiedLevel = Level.Info;
                    break;
                case LogVerbosity.Debug:
                    modifiedLevel = Level.Debug;
                    break;
                case LogVerbosity.Trace:
                    modifiedLevel = Level.Trace;
                    break;
                case LogVerbosity.Verbose:
                    modifiedLevel = Level.Verbose;
                    break;
                case LogVerbosity.All:
                    modifiedLevel = Level.All;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var repository = (Hierarchy)LogManager.GetRepository();
            repository.Root.Level = modifiedLevel;
            repository.Threshold = modifiedLevel;

            // the quiet option limits the amount output we send to the console
            // but the full leg level is still sent to log files
            var appenders = repository.GetAppenders();
            foreach (var appender in appenders)
            {
                if (appender is ConsoleAppender || appender is ManagedColoredConsoleAppender
                                                || appender is ColoredConsoleAppender)
                {
                    ((AppenderSkeleton)appender).Threshold = quietConsole ? Level.Error : modifiedLevel;
                }
            }

            repository.RaiseConfigurationChanged(EventArgs.Empty);

            Log.Debug("Log level changed to: " + logVerbosity);

            // log test
            //            Log.Debug("Log test DEBUG");
            //            Log.Info("Log test INFO");
            //            Log.Success("Log test SUCCESS");
            //            Log.Warn("Log test WARN");
            //            Log.Error("Log test ERROR");
            //            Log.Fatal("Log test FATAL");
            //            Log.Trace("Log test TRACE");
            //            Log.Verbose("Log test VERBOSE");
            //            LoggedConsole.Log.Info("Clean log INFO");
            //            LoggedConsole.Log.Success("Clean log SUCCESS");
            //            LoggedConsole.Log.Warn("Clean log WARN");
            //            LoggedConsole.Log.Error("Clean log ERROR");
            //            LoggedConsole.WriteLine("Clean wrapper INFO");
            //            LoggedConsole.WriteSuccessLine("Clean wrapper SUCCESS");
            //            LoggedConsole.WriteWarnLine("Clean wrapper WARN");
            //            LoggedConsole.WriteErrorLine("Clean wrapper ERROR");
            //            LoggedConsole.WriteFatalLine("Clean wrapper FATAL", new Exception("I'm a fake"));
        }

        internal static void AttachDebugger(ref DebugOptions options)
        {
            if (options == DebugOptions.No)
            {
                return;
            }
#if DEBUG
            if (!Debugger.IsAttached)
            {
                if (options == DebugOptions.Prompt)
                {
                    var response = Prompt.GetYesNo(
                        "Do you wish to debug? Attach now or press [Y] to attach. Press [N] or [ENTER] to continue.",
                        defaultAnswer: false,
                        promptColor: ConsoleColor.Cyan);
                    options = response ? DebugOptions.Yes : DebugOptions.No;
                }

                if (options == DebugOptions.Yes)
                {
                    var vsProcess =
                        VisualStudioAttacher.GetVisualStudioForSolutions(
                            new List<string> { "AudioAnalysis.sln" });

                    if (vsProcess != null)
                    {
                        VisualStudioAttacher.AttachVisualStudioToProcess(vsProcess, Process.GetCurrentProcess());
                    }
                    else
                    {
                        // try and attach the old fashioned way
                        Debugger.Launch();
                    }

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (Debugger.IsAttached)
                    {
                        LoggedConsole.WriteLine("\t>>> Attach sucessful");
                    }
                }

                LoggedConsole.WriteLine();
            }
#endif
        }

        internal static void BeforeExecute(MainArgs main, CommandLineApplication application)
        {
            // re-assign here... the application will be a sub-command here (which is tecnically a different CLA)
            CommandLineApplication = application;

            var debugOptions = main.DebugOption;
            AttachDebugger(ref debugOptions);

            ModifyVerbosity(main);

            Log.Debug($"Metric reporting is {(ApMetricRecording ? "en" : "dis")}abled.");

            LoadNativeCode();
        }

        internal static void Copyright()
        {
            LoggedConsole.WriteLine(
                // ReSharper disable once UnreachableCode
                $@"{Meta.Description} - version {BuildMetadata.VersionString} ({(InDEBUG ? "DEBUG" : "RELEASE")} build, {BuildMetadata.BuildDate})
Git branch-version: {BuildMetadata.GitBranch}-{BuildMetadata.GitCommit}, DirtyBuild:{BuildMetadata.IsDirty}, CI:{BuildMetadata.CiBuild}
Copyright {Meta.NowYear} {Meta.Organization}");
        }

        /// <summary>
        /// This method will stop the program from exiting if the solution was built in #DEBUG
        /// and the program was started by Visual Studio.
        /// </summary>
        [Conditional("DEBUG")]
        internal static void HangBeforeExit()
        {
#if DEBUG
            if (AppConfigHelper.IsMono)
            {
                return;
            }

            // if Michael is debugging with visual studio, this will prevent the window closing.
            Process parentProcess = ProcessExtensions.ParentProcessUtilities.GetParentProcess();
            if (parentProcess.ProcessName == "devenv")
            {
                LoggedConsole.WriteSuccessLine("FINISHED: Press RETURN key to exit.");
                Console.ReadLine();
            }
#endif
        }

        internal static void PrintUsage(string message, Usages usageStyle, string commandName = null)
        {
            //Contract.Requires(usageStyle != Usages.Single || commandName != null);

            var root = CommandLineApplication.Root();

            if (!string.IsNullOrWhiteSpace(message))
            {
                LoggedConsole.WriteErrorLine(message);
            }

            if (usageStyle == Usages.All)
            {
                // print entire usage
                root.ShowHelp();
            }
            else if (usageStyle == Usages.Single)
            {
                CommandLineApplication command;
                if (commandName == root.Name)
                {
                    command = root;
                }
                else
                {
                    command = root.Commands.FirstOrDefault(x =>
                        x.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));

                    // sometimes this is called from AppDomainUnhandledException, in which case throwing another exception
                    // just gets squashed!
                    if (command == null)
                    {
                        var commandNotFoundMessage = $"Could not find a command with name that matches `{commandName}`.";
                        Log.Fatal(commandNotFoundMessage);

                        throw new CommandParsingException(CommandLineApplication, commandNotFoundMessage);
                    }
                }

                command.ShowHelp();
            }
            else if (usageStyle == Usages.ListAvailable)
            {
                var commands = root.Commands;

                using (var sb = new StringWriter())
                {
                    ((CustomHelpTextGenerator)CommandLineApplication.HelpTextGenerator).FormatCommands(sb, commands);

                    LoggedConsole.WriteLine(sb.ToString());
                }
            }
            else if (usageStyle == Usages.NoAction)
            {
                CommandLineApplication.ShowHint();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static void AttachExceptionHandler()
        {
            Environment.ExitCode = ExceptionLookup.SpecialExceptionErrorLevel;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }

        private static CommandLineApplication CreateCommandLineApplication()
        {
            var console = PhysicalConsoleLogger.Default;
            var app = CommandLineApplication = new CommandLineApplication<MainArgs>(console);

            app.HelpTextGenerator = new CustomHelpTextGenerator { EnvironmentOptions = EnvironmentOptions };
            app.ValueParsers.Add(new DateTimeOffsetParser());
            app.ValueParsers.Add(new TimeSpanParser());
            app.ValueParsers.Add(new FileInfoParser());
            app.ValueParsers.Add(new DirectoryInfoParser());
            app.Conventions.UseDefaultConventions();

            return app;
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            Contract.Requires(unhandledExceptionEventArgs != null);
            Contract.Requires(unhandledExceptionEventArgs.ExceptionObject != null);

            const string fatalMessage = "Fatal error:\n  ";

            var ex = (Exception)unhandledExceptionEventArgs.ExceptionObject;

            ExceptionLookup.ExceptionStyle style;
            bool found;
            Exception inner = ex;

            // TODO: it looks like all exceptions will always be wrapped in a TargetInvocationException now so we always want to unwrap at least once
            switch (ex)
            {
                case TargetInvocationException _:
                case AggregateException _:
                    // unwrap
                    inner = ex.InnerException ?? ex;
                    Log.Debug($"Unwrapped {ex.GetType().Name} exception to show a  {inner.GetType().Name}");
                    found = ExceptionLookup.ErrorLevels.TryGetValue(inner.GetType(), out style);
                    break;
                default:
                    found = ExceptionLookup.ErrorLevels.TryGetValue(ex.GetType(), out style);
                    break;
            }

            found = found && style.Handle;

            // if found, print message only if usage printing disabled
            if (found && !style.PrintUsage)
            {
                // this branch prints the message, but the stack trace is only output in the log
                NoConsole.Log.Fatal(fatalMessage, ex);
                LoggedConsole.WriteFatalLine(fatalMessage + inner.Message);
            }
            else if (found && ex.GetType() != typeof(Exception))
            {
                // this branch prints the message, and command usage, but the stack trace is only output in the log
                NoConsole.Log.Fatal(fatalMessage, ex);

                var command = CommandLineApplication?.Name;
                var message = fatalMessage + inner.Message;
                PrintUsage(message, Usages.Single, command ?? string.Empty);
            }
            else
            {
                // otherwise its a unhandled exception, log and raise
                // trying to print cleaner errors in console, so printing a full one to log, and the inner to the console
                // this results in duplication in the log though
                NoConsole.Log.Fatal("Unhandled exception ->\n", ex);
                Log.Fatal("Unhandled exception ->\n", inner);

                PrintAggregateException(ex);
            }

            int returnCode = style?.ErrorCode ?? ExceptionLookup.SpecialExceptionErrorLevel;

            // finally return error level
            NoConsole.Log.Info("ERRORLEVEL: " + returnCode);
            if (Debugger.IsAttached)
            {
                // no don't exit, we want the exception to be raised to Window's Exception handling
                // this will allow the debugger to appropriately break on the right line
                Environment.ExitCode = returnCode;
            }
            else
            {
                // If debugger is not attached, we *do not* want to raise the error to the Windows level
                // Everything has already been logged, just exit with appropriate errorlevel
                Environment.Exit(returnCode);
            }
        }

        /// <summary>
        /// This method is used to do application wide loading of native code.
        /// </summary>
        /// <remarks>
        /// Until we convert this application to a .NET Core, there is no support for "runtimes" backed into the build
        /// system. Thus instead we:
        /// - copy runtimes manually as a build step
        ///   (due to a mono bug, the folder to copy in is named `libruntimes`. See https://github.com/libgit2/libgit2sharp/issues/1170)
        /// - map Dlls to their appropriate native DLLs in the dllmap entried in the App.config (which is used by the
        ///   mono runtime
        /// - and finally, call any intialization code that is needed here in this method.
        /// </remarks>
        private static void LoadNativeCode()
        {
            Log.Debug("Loading native code");

            // for sqlite
            // note: a custom dll map for sqlite can be found in SQLitePCLRaw.provider.e_sqlite3.dll.config
            SQLitePCL.Batteries_V2.Init();
        }

        private static void LogProgramStats()
        {
            var thisProcess = Process.GetCurrentProcess();
            var stats = new
            {
                Platform = Environment.OSVersion.ToString(),
                Environment.ProcessorCount,
                ExecutionTime = (DateTime.Now - thisProcess.StartTime).TotalSeconds,
                PeakWorkingSet = thisProcess.PeakWorkingSet64,
            };

            var statsString = "Programs stats:\n" + Json.SerialiseToString(stats, prettyPrint: true);

            NoConsole.Log.Info(statsString);
        }

        private static void ModifyVerbosity(MainArgs arguments)
        {
            SetLogVerbosity(arguments.LogLevel, arguments.QuietConsole);
        }

        private static void ParseEnvirionemnt()
        {
            ApPlainLogging = bool.TryParse(Environment.GetEnvironmentVariable(ApPlainLoggingKey), out var isTrue) && isTrue;
            var repository = (Hierarchy)LogManager.GetRepository();
            var root = repository.Root;
            var cleanLogger = (Logger)repository.GetLogger("CleanLogger");

            if (ApPlainLogging)
            {
                root.RemoveAppender("ConsoleAppender");
                cleanLogger.RemoveAppender("CleanConsoleAppender");
            }
            else
            {
                root.RemoveAppender("SimpleConsoleAppender");
            }

            if (bool.TryParse(Environment.GetEnvironmentVariable(ApMetricsKey), out var parseMetrics))
            {
                ApMetricRecording = parseMetrics;
            }
            else
            {
                // if the env var is not set or not parseable, then set default value.
                ApMetricRecording = true;
            }
        }

        private static void PrintAggregateException(Exception ex, int depth = 0)
        {
            var depthString = "==".PadLeft(depth * 2, '=');

            //innerExceptions = innerExceptions ?? new StringBuilder();

            if (ex is AggregateException)
            {
                var aex = (AggregateException)ex;

                //innerExceptions.AppendLine("Writing detailed information about inner exceptions!");

                foreach (var exception in aex.InnerExceptions)
                {
                    //innerExceptions.AppendLine();
                    Log.Fatal("\n\n" + depthString + "> Inner exception:", exception);

                    if (exception is AggregateException)
                    {
                        PrintAggregateException(exception, depth++);
                    }
                }
            }
        }
    }
}