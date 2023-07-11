using MewtocolNet.Exceptions;
using MewtocolNet.RegisterAttributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static PostInit<IPlcEthernet> Ethernet (string ip, int port = 9094, int station = 1) {

            var instance = new MewtocolInterfaceTcp();
            instance.ConfigureConnection(ip, port, station);
            return new PostInit<IPlcEthernet> {
                intf = instance
            };

        }

        /// <summary>
        /// Builds a ethernet based Mewtocol Interface
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="station">Plc station number</param>
        /// <returns></returns>
        public static PostInit<IPlcEthernet> Ethernet(IPAddress ip, int port = 9094, int station = 1) {

            var instance = new MewtocolInterfaceTcp();
            instance.ConfigureConnection(ip, port, station);
            return new PostInit<IPlcEthernet> {
                intf = instance
            };

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
        public static PostInit<IPlcSerial> Serial (string portName, BaudRate baudRate = BaudRate._19200, DataBits dataBits = DataBits.Eight, Parity parity = Parity.Odd, StopBits stopBits = StopBits.One, int station = 1) {

            TestPortName(portName);

            var instance = new MewtocolInterfaceSerial();
            instance.ConfigureConnection(portName, (int)baudRate, (int)dataBits, parity, stopBits, station);
            return new PostInit<IPlcSerial> {
                intf = instance
            };

        }

        /// <summary>
        /// Builds a serial mewtocol interface that finds the correct settings for the given port name automatically
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="station"></param>
        /// <returns></returns>
        public static PostInit<IPlcSerial> SerialAuto (string portName, int station = 1) {

            TestPortName(portName);

            var instance = new MewtocolInterfaceSerial();
            instance.ConfigureConnection(portName, station);
            instance.ConfigureConnectionAuto();
            return new PostInit<IPlcSerial> {
                intf = instance
            };

        }

        private static void TestPortName (string portName) {

            var portnames = SerialPort.GetPortNames();

            if (!portnames.Any(x => x == portName))
                throw new MewtocolException($"The port {portName} is no valid port");

        }

        public class MemoryManagerSettings {

            /// <summary>
            /// <code>
            /// This feature can improve read write times by a big margin but also
            /// block outgoing messages inbetween polling cycles more frequently
            /// </code>
            /// The max distance of the gap between registers (if there is a gap between 
            /// adjacent registers) to merge them into one request <br/>
            /// Example: <br/>
            /// <example>
            /// We have a register at DT100 (1 word long) and a
            /// register at DT101 (1 word long) <br/>
            /// - If the max distance is 0 it will not merge them into one request<br/>
            /// - If the max distance is 1 it will merge them into one request<br/>
            /// - If the max distance is 2 and the next register is at DT102 it will also merge them and ignore the spacer byte in the response<br/>
            /// </example>
            /// </summary>

            public int MaxOptimizationDistance { get; set; } = 8;

            /// <summary>
            /// The max number of registers per request group
            /// </summary>
            public int MaxRegistersPerGroup { get; set; } = -1;

        }

        public class PostInit<T> {

            internal T intf;

            /// <summary>
            /// Attaches a auto poller to the interface that reads all registers
            /// cyclic
            /// </summary>
            /// <returns></returns>
            public PostInit<T> WithPoller() {

                if (intf is MewtocolInterface imew) {
                    imew.usePoller = true;
                }

                return this;

            }

            /// <summary>
            /// General setting for the memory manager
            /// </summary>
            public PostInit<T> WithMemoryManagerSettings (Action<MemoryManagerSettings> settings) {

                var res = new MemoryManagerSettings();
                settings.Invoke(res);

                if (res.MaxOptimizationDistance < 0)
                    throw new NotSupportedException($"A value lower than 0 is not allowed for " +
                        $"{nameof(MemoryManagerSettings.MaxOptimizationDistance)}");

                if (intf is MewtocolInterface imew) {

                    imew.memoryManager.maxOptimizationDistance = res.MaxOptimizationDistance;
                    imew.memoryManager.maxRegistersPerGroup = res.MaxRegistersPerGroup;

                }

                return this;

            }

            /// <summary>
            /// A builder for attaching register collections
            /// </summary>
            public EndInit<T> WithRegisterCollections(Action<RegisterCollectionCollector> collector) {

                var res = new RegisterCollectionCollector();
                collector.Invoke(res);

                if (intf is MewtocolInterface imew) {
                    imew.WithRegisterCollections(res.collections);
                }

                return new EndInit<T> {
                    postInit = this
                };

            }

            /// <summary>
            /// Builds and returns the final plc interface
            /// </summary>
            public T Build() => intf;

        }

        public class EndInit<T> {

            internal PostInit<T> postInit;

            /// <summary>
            /// Builds and returns the final plc interface
            /// </summary>
            public T Build() => postInit.intf;

        }

    }

}
