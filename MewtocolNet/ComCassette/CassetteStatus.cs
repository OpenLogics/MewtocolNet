namespace MewtocolNet.ComCassette {

    /// <summary>
    /// Needs a list of all status codes.. hard to reverse engineer
    /// </summary>
    public enum CassetteStatus {

        /// <summary>
        /// Cassette is running as intended
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Cassette DHCP resolution error
        /// </summary>
        DHCPError = 2,

    }

}
