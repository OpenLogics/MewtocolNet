using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.Logging {

    /// <summary>
    /// Logging module for all PLCs
    /// </summary>
    public static class Logger {

        /// <summary>
        /// Sets the loglevel for the logger module
        /// </summary>
        public static LogLevel LogLevel { get; set; }   

        internal static Action<DateTime, string> LogInvoked;

        /// <summary>
        /// Gets invoked whenever a new log message is ready
        /// </summary>
        public static void OnNewLogMessage (Action<DateTime, string> onMsg) {

            LogInvoked += (t, m) => {
                onMsg(t, m);
            };

        }

        internal static void Log (string message, LogLevel loglevel, MewtocolInterface sender = null) {

            if ((int)loglevel <= (int)LogLevel) {
                if (sender == null) {
                    LogInvoked?.Invoke(DateTime.Now, message);
                } else {
                    LogInvoked?.Invoke(DateTime.Now, $"[{sender.GetConnectionPortInfo()}] {message}");
                }
            }

        }

    }
}
