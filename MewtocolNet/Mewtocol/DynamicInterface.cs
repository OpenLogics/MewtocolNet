using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MewtocolNet.Logging;
using MewtocolNet.Responses;

namespace MewtocolNet {

    /// <summary>
    /// The PLC com interface class
    /// </summary>
    public partial class MewtocolInterface {

        internal event Action PolledCycle;
        internal CancellationTokenSource cTokenAutoUpdater;
        internal bool ContinousReaderRunning;
        internal bool usePoller = false;

        #region Register Polling

        internal void KillPoller () {

            ContinousReaderRunning = false;
            cTokenAutoUpdater.Cancel();

        }

        /// <summary>
        /// Attaches a continous reader that reads back the Registers and Contacts
        /// </summary>
        internal void AttachPoller () {

            if (ContinousReaderRunning) return;

            cTokenAutoUpdater = new CancellationTokenSource();

            Logger.Log("Poller is attaching", LogLevel.Info, this);

            try {

                Task.Factory.StartNew(async () => {

                    var plcinf = await GetPLCInfoAsync();
                    if (plcinf == null) {
                        Logger.Log("PLC not reachable, stopping logger", LogLevel.Info, this);
                        return;
                    }

                    PolledCycle += MewtocolInterface_PolledCycle;
                    void MewtocolInterface_PolledCycle () {

                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (var reg in GetAllRegisters()) {
                            string address = $"{reg.GetRegisterString()}{reg.GetStartingMemoryArea()}".PadRight(8, (char)32);
                            stringBuilder.AppendLine($"{address}{(reg.Name != null ? $" ({reg.Name})" : "")}: {reg.GetValueString()}");
                        }

                        Logger.Log($"Registers loaded are: \n" +
                                   $"--------------------\n" +
                                   $"{stringBuilder.ToString()}" +
                                   $"--------------------",
                                   LogLevel.Verbose, this);

                        Logger.Log("Logger did its first cycle successfully", LogLevel.Info, this);

                        PolledCycle -= MewtocolInterface_PolledCycle;
                    }

                    ContinousReaderRunning = true;

                    while (ContinousReaderRunning) {

                        //do priority tasks first
                        if (PriorityTasks.Count > 0) {

                            await PriorityTasks.FirstOrDefault(x => !x.IsCompleted);

                        }

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
                            }
                            if (reg is NRegister<TimeSpan> tsReg) {
                                var lastVal = tsReg.Value;
                                var readout = (await ReadNumRegister(tsReg, stationNumber)).Register.Value;
                                if (lastVal != readout) {
                                    tsReg.LastValue = readout;
                                    InvokeRegisterChanged(tsReg);
                                    tsReg.TriggerNotifyChange();
                                }
                            }
                            if (reg is BRegister boolReg) {
                                var lastVal = boolReg.Value;
                                var readout = (await ReadBoolRegister(boolReg, stationNumber)).Register.Value;
                                if (lastVal != readout) {
                                    boolReg.LastValue = readout;
                                    InvokeRegisterChanged(boolReg);
                                    boolReg.TriggerNotifyChange();
                                }
                            }
                            if (reg is SRegister stringReg) {
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

                }, cTokenAutoUpdater.Token);

            } catch (TaskCanceledException) { }  

        }

        #endregion

        #region Register Adding


        /// <summary>
        /// Adds a PLC memory register to the watchlist <para/>
        /// The registers can be read back by attaching <see cref="WithPoller"/>
        /// </summary>
        /// <param name="_address">The address of the register in the PLCs memory</param>
        /// <param name="_type">
        /// The memory area type
        /// <para>X = Physical input area (bool)</para>
        /// <para>Y = Physical input area (bool)</para>
        /// <para>R = Internal relay area (bool)</para>
        /// <para>DT = Internal data area (short/ushort)</para>
        /// <para>DDT = Internal relay area (int/uint)</para>
        /// </param>
        /// <param name="_name">A naming definition for QOL, doesn't effect PLC and is optional</param>
        public void AddRegister (int _address, RegisterType _type, string _name = null) {

            //as number registers
            if (_type == RegisterType.DT_short) {
                Registers.Add(_address, new NRegister<short>(_address, _name));
                return;
            }
            if (_type == RegisterType.DT_ushort) {
                Registers.Add(_address, new NRegister<ushort>(_address, _name));
                return;
            }
            if (_type == RegisterType.DDT_int) {
                Registers.Add(_address, new NRegister<int>(_address, _name));
                return;
            }
            if (_type == RegisterType.DDT_uint) {
                Registers.Add(_address, new NRegister<uint>(_address, _name));
                return;
            }
            if (_type == RegisterType.DDT_float) {
                Registers.Add(_address, new NRegister<float>(_address, _name));
                return;
            }

            //as bool registers
            Registers.Add(_address, new BRegister(_address, _type, _name));

        }

        /// <summary>
        /// Adds a PLC memory register to the watchlist <para/>
        /// The registers can be read back by attaching <see cref="WithPoller"/>
        /// </summary>
        /// <param name="_spAddress">The special address of the register in the PLCs memory</param>
        /// <param name="_type">
        /// The memory area type
        /// <para>X = Physical input area (bool)</para>
        /// <para>Y = Physical input area (bool)</para>
        /// <para>R = Internal relay area (bool)</para>
        /// <para>DT = Internal data area (short/ushort)</para>
        /// <para>DDT = Internal relay area (int/uint)</para>
        /// </param>
        /// <param name="_name">A naming definition for QOL, doesn't effect PLC and is optional</param>
        public void AddRegister (SpecialAddress _spAddress, RegisterType _type, string _name = null) {

            //as bool registers
            Registers.Add((int)_spAddress, new BRegister(_spAddress, _type, _name));

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
        public void AddRegister<T>(int _address, int _length = 1, string _name = null, bool _isBitwise = false) {

            Type regType = typeof(T);

            if (regType != typeof(string) && _length != 1) {
                throw new NotSupportedException($"_lenght parameter only allowed for register of type string");
            }

            if (Registers.Any(x => x.Key == _address)) {

                throw new NotSupportedException($"Cannot add a register multiple times, " +
                    $"make sure that all register attributes or AddRegister assignments have different adresses.");

            }

            if (regType == typeof(short)) {
                Registers.Add(_address, new NRegister<short>(_address, _name, _isBitwise));
            } else if (regType == typeof(ushort)) {
                Registers.Add(_address, new NRegister<ushort>(_address, _name));
            } else if (regType == typeof(int)) {
                Registers.Add(_address,  new NRegister<int>(_address, _name, _isBitwise));
            } else if (regType == typeof(uint)) {
                Registers.Add(_address,  new NRegister<uint>(_address, _name));
            } else if (regType == typeof(float)) {
                Registers.Add(_address,  new NRegister<float>(_address, _name));
            } else if (regType == typeof(string)) {
                Registers.Add(_address,  new SRegister(_address, _length, _name));
            } else if (regType == typeof(TimeSpan)) {
                Registers.Add(_address, new NRegister<TimeSpan>(_address, _name));
            } else if (regType == typeof(bool)) {
                Registers.Add(_address, new BRegister(_address, RegisterType.R, _name));
            } else {
                throw new NotSupportedException($"The type {regType} is not allowed for Registers \n" +
                                                $"Allowed are: short, ushort, int, uint, float and string");
            }

        }

        #endregion

        #region Register Reading

        /// <summary>
        /// Gets a list of all added registers
        /// </summary>
        public List<Register> GetAllRegisters () {

            return Registers.Values.ToList();

        }

        #endregion

        #region Event Invoking 

        internal void InvokeRegisterChanged (Register reg) {

            RegisterChanged?.Invoke(reg);      

        }

        internal void InvokePolledCycleDone () {

            PolledCycle?.Invoke();    

        }

        #endregion

    }
}
