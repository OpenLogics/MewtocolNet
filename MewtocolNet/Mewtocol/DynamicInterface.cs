using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MewtocolNet.Responses;

namespace MewtocolNet {

    /// <summary>
    /// The PLC com interface class
    /// </summary>
    public partial class MewtocolInterface {

        internal event Action PolledCycle;
        internal CancellationTokenSource cTokenAutoUpdater;
        internal bool isWriting;
        internal bool ContinousReaderRunning;
        internal bool usePoller = false;

        #region Register Polling

        /// <summary>
        /// Attaches a continous reader that reads back the Registers and Contacts
        /// </summary>
        internal void AttachPoller () {

            if (ContinousReaderRunning) return;

            cTokenAutoUpdater = new CancellationTokenSource();

            Console.WriteLine("Attaching cont reader");

            Task.Factory.StartNew(async () => {

                var plcinf = await GetPLCInfoAsync();
                if (plcinf == null) {
                    Console.WriteLine("PLC is not reachable");
                    throw new Exception("PLC is not reachable");
                }
                if (!plcinf.OperationMode.RunMode) {
                    Console.WriteLine("PLC is not running");
                    throw new Exception("PLC is not running");
                }

                ContinousReaderRunning = true;

                while (true) {

                    //dont update when currently writing a var
                    if (isWriting) {
                        continue;
                    }

                    await Task.Delay(pollingDelayMs);
                    foreach (var registerPair in Registers) {

                        var reg = registerPair.Value;

                        if (reg is NRegister<short> shortReg) {
                            var lastVal = shortReg.Value;
                            var readout = (await ReadNumRegister(shortReg, stationNumber)).Register.Value;
                            if (lastVal != readout) {
                                shortReg.LastValue = readout;
                                InvokeRegisterChanged(shortReg);
                                shortReg.TriggerNotifyChange();
                            }
                        }
                        if (reg is NRegister<ushort> ushortReg) {
                            var lastVal = ushortReg.Value;
                            var readout = (await ReadNumRegister(ushortReg, stationNumber)).Register.Value;
                            if (lastVal != readout) {
                                ushortReg.LastValue = readout;
                                InvokeRegisterChanged(ushortReg);
                                ushortReg.TriggerNotifyChange();
                            }
                        }
                        if (reg is NRegister<int> intReg) {
                            var lastVal = intReg.Value;
                            var readout = (await ReadNumRegister(intReg, stationNumber)).Register.Value;
                            if (lastVal != readout) {
                                intReg.LastValue = readout;
                                InvokeRegisterChanged(intReg);
                                intReg.TriggerNotifyChange();
                            }
                        }
                        if (reg is NRegister<uint> uintReg) {
                            var lastVal = uintReg.Value;
                            var readout = (await ReadNumRegister(uintReg, stationNumber)).Register.Value;
                            if (lastVal != readout) {
                                uintReg.LastValue = readout;
                                InvokeRegisterChanged(uintReg);
                                uintReg.TriggerNotifyChange();
                            }
                        }
                        if (reg is NRegister<float> floatReg) {
                            var lastVal = floatReg.Value;
                            var readout = (await ReadNumRegister(floatReg, stationNumber)).Register.Value;
                            if (lastVal != readout) {
                                floatReg.LastValue = readout;
                                InvokeRegisterChanged(floatReg);
                                floatReg.TriggerNotifyChange();
                            }
                        } else if (reg is SRegister stringReg) {
                            var lastVal = stringReg.Value;
                            var readout = (await ReadStringRegister(stringReg, stationNumber)).Register.Value;
                            if (lastVal != readout) {
                                InvokeRegisterChanged(stringReg);
                                stringReg.TriggerNotifyChange();
                            }

                        }

                    }

                    //invoke cycle polled event
                     InvokePolledCycleDone();

                }

            },  cTokenAutoUpdater.Token);

        }

        #endregion

        #region Register Adding

        /// <summary>
        /// Adds a PLC memory register to the watchlist <para/>
        /// The registers can be read back by attaching <see cref="WithPoller"/>
        /// </summary>
        /// <typeparam name="T">
        /// The type of the register translated from C# to IEC 61131-3 types
        /// <para>C# ------ IEC</para>
        /// <para>short => INT/WORD</para>
        /// <para>ushort => UINT</para>
        /// <para>int => DOUBLE</para>
        /// <para>uint => UDOUBLE</para>
        /// <para>float => REAL</para>
        /// <para>string => STRING</para>
        /// </typeparam>
        /// <param name="_address">The address of the register in the PLCs memory</param>
        /// <param name="_length">The length of the string (Can be ignored for other types)</param>
        public void AddRegister<T> (int _address, int _length = 1) {

            Type regType = typeof(T);

            if (regType == typeof(short)) {
                Registers.Add(_address, new NRegister<short>(_address));
            } else if (regType == typeof(ushort)) {
                Registers.Add(_address, new NRegister<ushort>(_address));
            } else if (regType == typeof(int)) {
                Registers.Add(_address, new NRegister<int>(_address));
            } else if (regType == typeof(uint)) {
                Registers.Add(_address, new NRegister<uint>(_address));
            } else if (regType == typeof(float)) {
                Registers.Add(_address, new NRegister<float>(_address));
            } else if (regType == typeof(string)) {
                Registers.Add(_address, new SRegister(_address, _length));
            } else {
                throw new NotSupportedException($"The type {regType} is not allowed for Registers \n" +
                                                $"Allowed are: short, ushort, int, uint, float and string");
            }

        }

        /// <summary>
        /// Adds a PLC memory register to the watchlist <para/>
        /// The registers can be read back by attaching <see cref="WithPoller"/>
        /// </summary>
        /// <typeparam name="T">
        /// The type of the register translated from C# to IEC 61131-3 types
        /// <para>C# ------ IEC</para>
        /// <para>short => INT/WORD</para>
        /// <para>ushort => UINT</para>
        /// <para>int => DOUBLE</para>
        /// <para>uint => UDOUBLE</para>
        /// <para>float => REAL</para>
        /// <para>string => STRING</para>
        /// </typeparam>
        /// <param name="_name">A naming definition for QOL, doesn't effect PLC and is optional</param>
        /// <param name="_address">The address of the register in the PLCs memory</param>
        /// <param name="_length">The length of the string (Can be ignored for other types)</param>
        public void AddRegister<T>(string _name, int _address, int _length = 1) {

            Type regType = typeof(T);

            if (regType == typeof(short)) {
                Registers.Add(_address, new NRegister<short>(_address, _name));
            } else if (regType == typeof(ushort)) {
                Registers.Add(_address, new NRegister<ushort>(_address, _name));
            } else if (regType == typeof(int)) {
                Registers.Add(_address,  new NRegister<int>(_address, _name));
            } else if (regType == typeof(uint)) {
                Registers.Add(_address,  new NRegister<uint>(_address, _name));
            } else if (regType == typeof(float)) {
                Registers.Add(_address,  new NRegister<float>(_address, _name));
            } else if (regType == typeof(string)) {
                Registers.Add(_address,  new SRegister(_address, _length, _name));
            } else {
                throw new NotSupportedException($"The type {regType} is not allowed for Registers \n" +
                                                $"Allowed are: short, ushort, int, uint, float and string");
            }

        }

        #endregion

        internal void InvokeRegisterChanged (Register reg) {

            RegisterChanged?.Invoke(reg);      

        }

        internal void InvokePolledCycleDone () {

            PolledCycle?.Invoke();    

        }

    }
}
