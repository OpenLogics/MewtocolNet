using MewtocolNet.RegisterAttributes;
using System;
using System.Net;
using System.Threading.Tasks;

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
        /// The host ip endpoint, leave it null to use an automatic interface
        /// </summary>
        IPEndPoint HostEndpoint { get; set; }

        /// <summary>
        /// Tries to establish a connection with the device asynchronously
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Configures the serial interface
        /// </summary>
        /// <param name="_ip">IP adress of the PLC</param>
        /// <param name="_port">Port of the PLC</param>
        /// <param name="_station">Station Number of the PLC</param>
        void ConfigureConnection(string _ip, int _port = 9094, int _station = 1);

        /// <summary>
        /// Attaches a poller to the interface
        /// </summary>
        IPlcEthernet WithPoller();

        /// <summary>
        /// Attaches a register collection object to 
        /// the interface that can be updated automatically.
        /// </summary>
        /// <param name="collection">The type of the collection base class</param>
        IPlcEthernet AddRegisterCollection(RegisterCollection collection);

    }

}
