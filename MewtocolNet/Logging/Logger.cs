using System;

namespace MewtocolNet.Logging {

    /// <summary>
    /// Logging module for all PLCs
    /// </summary>
    public static class Logger {

        /// <summary>
        /// Sets the loglevel for the logger module
        /// </summary>
        public static LogLevel LogLevel { get; set; }

        internal static Action<DateTime, LogLevel, string> LogInvoked;

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
                    LogInvoked?.Invoke(DateTime.Now, loglevel, $"[{sender.GetConnectionPortInfo()}] {message}");
                }
            }

        }

    }
}
