using MewtocolNet.Exceptions;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;

namespace MewtocolNet {
    
    /// <summary>
    /// Builder helper for mewtocol interfaces
    /// </summary>
    public static class Mewtocol {
    
        /// <summary>
        /// Builds a ethernet based Mewtocol Interface
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="station">Plc station number</param>
        /// <returns></returns>
        public static IPlcEthernet Ethernet (string ip, int port = 9094, int station = 1) {

            var instance = new MewtocolInterfaceTcp();
            instance.ConfigureConnection(ip, port, station);
            return instance;

        }

        /// <summary>
        /// Builds a ethernet based Mewtocol Interface
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="station">Plc station number</param>
        /// <returns></returns>
        public static IPlcEthernet Ethernet(IPAddress ip, int port = 9094, int station = 1) {

            var instance = new MewtocolInterfaceTcp();
            instance.ConfigureConnection(ip, port, station);
            return instance;

        }

        /// <summary>
        /// Builds a serial port based Mewtocol Interface
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="dataBits"></param>
        /// <param name="parity"></param>
        /// <param name="stopBits"></param>
        /// <param name="station"></param>
        /// <returns></returns>
        public static IPlcSerial Serial (string portName, BaudRate baudRate = BaudRate._19200, DataBits dataBits = DataBits.Eight, Parity parity = Parity.Odd, StopBits stopBits = StopBits.One, int station = 1) {

            TestPortName(portName);

            var instance = new MewtocolInterfaceSerial();
            instance.ConfigureConnection(portName, (int)baudRate, (int)dataBits, parity, stopBits, station);
            return instance;

        }

        /// <summary>
        /// Builds a serial mewtocol interface that finds the correct settings for the given port name automatically
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="station"></param>
        /// <returns></returns>
        public static IPlcSerial SerialAuto (string portName, int station = 1) {

            TestPortName(portName);

            var instance = new MewtocolInterfaceSerial();
            instance.ConfigureConnection(portName, station);
            instance.ConfigureConnectionAuto();
            return instance;

        }

        private static void TestPortName (string portName) {

            var portnames = SerialPort.GetPortNames();

            if (!portnames.Any(x => x == portName))
                throw new MewtocolException($"The port {portName} is no valid port");

        }

    }

}
