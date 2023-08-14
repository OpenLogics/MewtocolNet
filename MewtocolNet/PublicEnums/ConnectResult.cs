namespace MewtocolNet {

    /// <summary>
    /// The result of an initial connection
    /// </summary>
    public enum ConnectResult {

        /// <summary>
        /// There is no known reason for the connection to fail
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The PLC did establish the connection as expected
        /// </summary>
        Connected = 1,
        /// <summary>
        /// The tcp/serial connection to the device timed out (PLC did not respond within time limits)
        /// </summary>
        Timeout = 2,
        /// <summary>
        /// The PLC sent a an error message on the type determination stage
        /// </summary>
        MewtocolError = 3,
        /// <summary>
        /// The metadata of the PLC did not match the required metadata for the interface
        /// </summary>
        MismatchMetadata = 4,

    }

}
