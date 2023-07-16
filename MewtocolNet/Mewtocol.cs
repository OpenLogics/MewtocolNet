using MewtocolNet.Exceptions;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.SetupClasses;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;

namespace MewtocolNet {

    /// <summary>
    /// Builder helper for mewtocol interfaces
    /// </summary>
    public static class Mewtocol {

        #region Build Order 1

        /// <summary>
        /// Builds a ethernet based Mewtocol Interface
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="station">Plc station number 0xEE for direct communication</param>
        /// <returns></returns>
        public static PostInit<IPlcEthernet> Ethernet(string ip, int port = 9094, int station = 0xEE) {

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
        /// <param name="station">Plc station number 0xEE for direct communication</param>
        /// <returns></returns>
        public static PostInit<IPlcEthernet> Ethernet(IPAddress ip, int port = 9094, int station = 0xEE) {

            var instance = new MewtocolInterfaceTcp();
            instance.ConfigureConnection(ip, port, station);
            return new PostInit<IPlcEthernet> {
                intf = instance
            };

        }

        /// <summary>
        /// Builds a serial port based Mewtocol Interface
        /// </summary>
        /// <param name="portName">System port name</param>
        /// <param name="baudRate">Baud rate of the plc toolport</param>
        /// <param name="dataBits">DataBits of the plc toolport</param>
        /// <param name="parity">Parity rate of the plc toolport</param>
        /// <param name="stopBits">Stop bits of the plc toolport</param>
        /// <param name="station">Plc station number 0xEE for direct communication</param>
        /// <returns></returns>
        public static PostInit<IPlcSerial> Serial(string portName, BaudRate baudRate = BaudRate._19200, DataBits dataBits = DataBits.Eight, Parity parity = Parity.Odd, StopBits stopBits = StopBits.One, int station = 0xEE) {

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
        /// <param name="station">Plc station number 0xEE for direct communication</param>
        /// <returns></returns>
        public static PostInit<IPlcSerial> SerialAuto(string portName, int station = 0xEE) {

            TestPortName(portName);

            var instance = new MewtocolInterfaceSerial();
            instance.ConfigureConnection(portName, station);
            instance.ConfigureConnectionAuto();
            return new PostInit<IPlcSerial> {
                intf = instance
            };

        }

        private static void TestPortName(string portName) {

            var portnames = SerialPort.GetPortNames();

            if (!portnames.Any(x => x == portName))
                throw new MewtocolException($"The port {portName} is no valid port");

        }

        #endregion

        #region Build Order 2

        public class PollLevelConfigurator {

            internal Dictionary<int, PollLevelConfig> levelConfigs = new Dictionary<int, PollLevelConfig>();

            /// <summary>
            /// Sets the poll level for the given key
            /// </summary>
            /// <param name="level">The level to reference</param>
            /// <param name="interval">Delay between poll requests</param>
            public PollLevelConfigurator SetLevel(int level, TimeSpan interval) {

                if (level <= 1)
                    throw new NotSupportedException($"The poll level {level} is not configurable");

                if (!levelConfigs.ContainsKey(level)) {
                    levelConfigs.Add(level, new PollLevelConfig {
                        delay = interval,
                    });
                } else {
                    throw new NotSupportedException("Can't set poll levels multiple times");
                }

                return this;

            }

            public PollLevelConfigurator SetLevel(int level, int skipNth) {

                if (level <= 1)
                    throw new NotSupportedException($"The poll level {level} is not configurable");

                if (!levelConfigs.ContainsKey(level)) {
                    levelConfigs.Add(level, new PollLevelConfig {
                        skipNth = skipNth,
                    });
                } else {
                    throw new NotSupportedException("Can't set poll levels multiple times");
                }

                return this;

            }

        }

        public class RegCollector {

            internal List<RegisterCollection> collections = new List<RegisterCollection>();

            public RegCollector AddCollection(RegisterCollection collection) {

                collections.Add(collection);

                return this;

            }

            public RegCollector AddCollection<T>() where T : RegisterCollection {

                var instance = (RegisterCollection)Activator.CreateInstance(typeof(T));

                collections.Add(instance);

                return this;

            }

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
            public PostInit<T> WithInterfaceSettings(Action<InterfaceSettings> settings) {

                var res = new InterfaceSettings();
                settings.Invoke(res);

                if (res.MaxOptimizationDistance < 0)
                    throw new NotSupportedException($"A value lower than 0 is not allowed for " +
                        $"{nameof(InterfaceSettings.MaxOptimizationDistance)}");

                if (res.MaxDataBlocksPerWrite < 1)
                    throw new NotSupportedException($"A value lower than 1 is not allowed for " +
                        $"{nameof(InterfaceSettings.MaxDataBlocksPerWrite)}");

                if (intf is MewtocolInterface imew) {

                    imew.memoryManager.maxOptimizationDistance = res.MaxOptimizationDistance;
                    imew.memoryManager.pollLevelOrMode = res.PollLevelOverwriteMode;

                    imew.maxDataBlocksPerWrite = res.MaxDataBlocksPerWrite; 

                }

                return this;

            }

            /// <summary>
            /// A builder for poll custom levels
            /// </summary>
            public PostInit<T> WithCustomPollLevels(Action<PollLevelConfigurator> levels) {

                var res = new PollLevelConfigurator();
                levels.Invoke(res);

                if (intf is MewtocolInterface imew) {

                    imew.memoryManager.pollLevelConfigs = res.levelConfigs;

                }

                return this;

            }

            /// <summary>
            /// A builder for attaching register collections
            /// </summary>
            public EndInit<T> WithRegisterCollections(Action<RegCollector> collector) {

                try {

                    var res = new RegCollector();
                    collector.Invoke(res);

                    if (intf is MewtocolInterface imew) {
                        imew.WithRegisterCollections(res.collections);
                    }

                    return new EndInit<T> {
                        postInit = this
                    };

                } catch {

                    throw;

                }

            }

            /// <summary>
            /// Builds and returns the final plc interface
            /// </summary>
            public T Build() => intf;

        }

        #endregion

        #region BuildLevel 3

        public class EndInit<T> {

            internal PostInit<T> postInit;

            /// <summary>
            /// Builds and returns the final plc interface
            /// </summary>
            public T Build() => postInit.intf;

        }

        #endregion

    }

}
