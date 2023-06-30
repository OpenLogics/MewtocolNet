using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace MewtocolNet {
    
    /// <summary>
    /// Builder helper for mewtocol interfaces
    /// </summary>
    public static class Mewtocol {
    
        /// <summary>
        /// Builds a ethernet based Mewtocol Interface
        /// </summary>
        /// <param name="_ip"></param>
        /// <param name="_port"></param>
        /// <param name="_station"></param>
        /// <returns></returns>
        public static IPlcEthernet Ethernet (string _ip, int _port = 9094, int _station = 1) {

            var instance = new MewtocolInterfaceTcp();
            instance.ConfigureConnection(_ip, _port, _station);
            return instance;

        }

        /// <summary>
        /// Builds a serial port based Mewtocol Interface
        /// </summary>
        /// <param name="_portName"></param>
        /// <param name="_baudRate"></param>
        /// <param name="_dataBits"></param>
        /// <param name="_parity"></param>
        /// <param name="_stopBits"></param>
        /// <returns></returns>
        public static IPlcSerial Serial (string _portName, BaudRate _baudRate = BaudRate._19200, DataBits _dataBits = DataBits.Eight, Parity _parity = Parity.Odd, StopBits _stopBits = StopBits.One, int _station = 1) {

            var instance = new MewtocolInterfaceSerial();
            instance.ConfigureConnection(_portName, (int)_baudRate, (int)_dataBits, _parity, _stopBits, _station);
            return instance;

        }

        /// <summary>
        /// Builds a serial mewtocol interface that finds the correct settings for the given port name automatically
        /// </summary>
        /// <param name="_portName"></param>
        /// <param name="_station"></param>
        /// <returns></returns>
        public static IPlcSerial SerialAuto (string _portName, int _station = 1) {

            var instance = new MewtocolInterfaceSerial();
            instance.ConfigureConnection(_portName, _station);
            instance.ConfigureConnectionAuto();
            return instance;

        }

    }

}
