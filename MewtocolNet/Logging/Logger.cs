﻿using System;
using System.Diagnostics;

namespace MewtocolNet.Logging {

    /// <summary>
    /// Logging module for all PLCs
    /// </summary>
    public static class Logger {

        /// <summary>
        /// Sets the loglevel for the logger module
        /// </summary>
        public static LogLevel LogLevel { get; set; }

        /// <summary>
        /// Defines the default output logger targets
        /// </summary>
        public static LoggerTargets DefaultTargets { get; set; } = LoggerTargets.Console;

        internal static Action<DateTime, LogLevel, string> LogInvoked;

        static Logger () {

            OnNewLogMessage((d, l, m) => {

                if(DefaultTargets.HasFlag(LoggerTargets.Console)) {

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

                if (DefaultTargets.HasFlag(LoggerTargets.Trace)) {

                    Trace.WriteLine($"{d:hh:mm:ss:ff} {m}");

                }

            });

        }

        //for static calling purposes only
        internal static void Start() { }

        /// <summary>
        /// Gets invoked whenever a new log message is ready
        /// </summary>
        public static void OnNewLogMessage(Action<DateTime, LogLevel, string> onMsg) {

            LogInvoked += (t, l, m) => {
                onMsg(t, l, m);
            };

        }

        internal static void Log(string message, LogLevel loglevel, MewtocolInterface sender = null) {

            if ((int)loglevel <= (int)LogLevel) {
                if (sender == null) {
                    LogInvoked?.Invoke(DateTime.Now, loglevel, message);
                } else {
                    LogInvoked?.Invoke(DateTime.Now, loglevel, $"[{sender.GetConnectionInfo()}] {message}");
                }
            }

        }

    }
}
