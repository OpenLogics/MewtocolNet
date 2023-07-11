using MewtocolNet.RegisterAttributes;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet {

    /// <summary>
    /// Provides a interface for Panasonic PLCs over a serial port connection
    /// </summary>
    public interface IPlcSerial : IPlc {

        /// <summary>
        /// Port name of the serial port that this device is configured for
        /// </summary>
        string PortName { get; }

        /// <summary>
        /// The serial connection baud rate that this device is configured for
        /// </summary>
        int SerialBaudRate { get; }

        /// <summary>
        /// The serial connection data bits
        /// </summary>
        int SerialDataBits { get; }

        /// <summary>
        /// The serial connection parity
        /// </summary>
        Parity SerialParity { get; }

        /// <summary>
        /// The serial connection stop bits
        /// </summary>
        StopBits SerialStopBits { get; }

        /// <summary>
        /// Sets up the connection settings for the device
        /// </summary>
        /// <param name="_portName">Port name of COM port</param>
        /// <param name="_baudRate">The serial connection baud rate</param>
        /// <param name="_dataBits">The serial connection data bits</param>
        /// <param name="_parity">The serial connection parity</param>
        /// <param name="_stopBits">The serial connection stop bits</param>
        /// <param name="_station">The station number of the PLC</param>
        void ConfigureConnection(string _portName, int _baudRate = 19200, int _dataBits = 8, Parity _parity = Parity.Odd, StopBits _stopBits = StopBits.One, int _station = 1);

        /// <summary>
        /// Tries to establish a connection with the device asynchronously
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Tries to establish a connection with the device asynchronously
        /// </summary>
        Task ConnectAsync(Action onTryingConfig);

    }

}
