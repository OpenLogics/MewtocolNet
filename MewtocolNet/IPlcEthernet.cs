namespace MewtocolNet {

    /// <summary>
    /// Provides a interface for Panasonic PLCs over a ethernet connection
    /// </summary>
    public interface IPlcEthernet : IPlc {

        /// <summary>
        /// The current IP of the PLC connection
        /// </summary>
        string IpAddress { get; }

        /// <summary>
        /// The current port of the PLC connection
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Attaches a poller to the interface
        /// </summary>
        public IPlcEthernet WithPoller();

        /// <summary>
        /// Configures the serial interface
        /// </summary>
        /// <param name="_ip">IP adress of the PLC</param>
        /// <param name="_port">Port of the PLC</param>
        /// <param name="_station">Station Number of the PLC</param>
        void ConfigureConnection(string _ip, int _port = 9094, int _station = 1);

    }

}
