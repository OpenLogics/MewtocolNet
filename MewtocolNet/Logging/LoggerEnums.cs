namespace MewtocolNet.Logging {

    /// <summary>
    /// The loglevel of the logging module
    /// </summary>
    public enum LogLevel {

        /// <summary>
        /// Logs only errors
        /// </summary>
        Error = 0,
        /// <summary>
        /// Logs info like connection establish and loss
        /// </summary>
        Info = 1,
        /// <summary>
        /// Logs only state changes
        /// </summary>
        Change = 2,
        /// <summary>
        /// Logs all errors, state changes, and messages
        /// </summary>
        Verbose = 3,
        /// <summary>
        /// Logs all types including network traffic
        /// </summary>
        Critical = 4,
    }

}
