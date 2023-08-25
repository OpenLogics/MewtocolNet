using System;
using System.Diagnostics;

namespace MewtocolNet.Logging {

    /// <summary>
    /// Logging module for all PLCs
    /// </summary>
    public static class Logger {

        /// <summary>
        /// Sets the loglevel for the global logging module
        /// </summary>
        public static LogLevel LogLevel { get; set; }

        /// <summary>
        /// Defines the default output logger targets
        /// </summary>
        public static LoggerTargets DefaultTargets { get; set; } = LoggerTargets.Console;

        internal static Action<DateTime, LogLevel, string> LogInvoked;

        static Logger () {

            var isConsoleApplication = Console.LargestWindowWidth != 0;

            OnNewLogMessage((d, l, m) => {

                if(isConsoleApplication && DefaultTargets.HasFlag(LoggerTargets.Console)) {

                    switch (l) {
                        case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{d:hh:mm:ss:ff} {m}");
                        break;
                        case LogLevel.Info:
                        Console.WriteLine($"{d:hh:mm:ss:ff} {m}");
                        break;
                        case LogLevel.Change:
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.WriteLine($"{d:hh:mm:ss:ff} {m}");
                        break;
                        case LogLevel.Verbose:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"{d:hh:mm:ss:ff} {m}");
                        break;
                        case LogLevel.Critical:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"{d:hh:mm:ss:ff} {m}");
                        break;
                    }

                    Console.ResetColor();

                }

            });

            LogInvoked += (d, l, m) => {

                if (DefaultTargets.HasFlag(LoggerTargets.Trace)) {

                    Trace.WriteLine($"{d:hh:mm:ss:ff} {m}");

                }

            };

        }

        //for static calling purposes only
        internal static void Start() { }

        /// <summary>
        /// Gets invoked whenever a new log message is ready
        /// </summary>
        public static void OnNewLogMessage(Action<DateTime, LogLevel, string> onMsg, LogLevel? maxLevel = null) {

            if (maxLevel == null) maxLevel = LogLevel;

            LogInvoked += (t, l, m) => {

                if ((int)l <= (int)maxLevel) {
                    onMsg(t, l, m);
                }

            };

        }

        internal static void Log(string message, LogLevel loglevel, MewtocolInterface sender = null) {

            if (sender == null) {
                LogInvoked?.Invoke(DateTime.Now, loglevel, message);
            } else {
                LogInvoked?.Invoke(DateTime.Now, loglevel, $"[{sender.GetConnectionInfo()}] {message}");
            }

        }

        internal static void LogError (string message, MewtocolInterface sender = null) => Log(message, LogLevel.Error, sender);

        internal static void Log (string message, MewtocolInterface sender = null) => Log(message, LogLevel.Info, sender);

        internal static void LogChange (string message, MewtocolInterface sender = null) => Log(message, LogLevel.Change, sender);

        internal static void LogVerbose (string message, MewtocolInterface sender = null) => Log(message, LogLevel.Verbose, sender);

        internal static void LogCritical (string message, MewtocolInterface sender = null) => Log(message, LogLevel.Critical, sender);

    }
}
