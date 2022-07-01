using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MewtocolNet.Logging;
using MewtocolNet.Registers;

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
            cTokenAutoUpdater?.Cancel();

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

                    int getPLCinfoCycleCount = 0;

                    while (ContinousReaderRunning) {

                        //do priority tasks first
                        if (PriorityTasks != null && PriorityTasks.Count > 0) {

                            try {
                                await PriorityTasks?.FirstOrDefault(x => !x.IsCompleted);
                            } catch (NullReferenceException) { }

                        } else if (getPLCinfoCycleCount > 25) {

                            await GetPLCInfoAsync();
                            getPLCinfoCycleCount = 0;

                        }

                        foreach (var registerPair in Registers) {

                            var reg = registerPair.Value;

                            if (reg is NRegister<short> shortReg) {
                                var lastVal = shortReg.Value;
                                var readout = (await ReadNumRegister(shortReg)).Register.Value;
                                if (lastVal != readout) {
                                    InvokeRegisterChanged(shortReg);
                                }
                            }
                            if (reg is NRegister<ushort> ushortReg) {
                                var lastVal = ushortReg.Value;
                                var readout = (await ReadNumRegister(ushortReg)).Register.Value;
                                if (lastVal != readout) {
                                    InvokeRegisterChanged(ushortReg);
                                }
                            }
                            if (reg is NRegister<int> intReg) {
                                var lastVal = intReg.Value;
                                var readout = (await ReadNumRegister(intReg)).Register.Value;
                                if (lastVal != readout) {
                                    InvokeRegisterChanged(intReg);
                                }
                            }
                            if (reg is NRegister<uint> uintReg) {
                                var lastVal = uintReg.Value;
                                var readout = (await ReadNumRegister(uintReg)).Register.Value;
                                if (lastVal != readout) {
                                    InvokeRegisterChanged(uintReg);
                                }
                            }
                            if (reg is NRegister<float> floatReg) {
                                var lastVal = floatReg.Value;
                                var readout = (await ReadNumRegister(floatReg)).Register.Value;
                                if (lastVal != readout) {
                                    InvokeRegisterChanged(floatReg);
                                }
                            }
                            if (reg is NRegister<TimeSpan> tsReg) {
                                var lastVal = tsReg.Value;
                                var readout = (await ReadNumRegister(tsReg)).Register.Value;
                                if (lastVal != readout) {
                                    InvokeRegisterChanged(tsReg);
                                }
                            }
                            if (reg is BRegister boolReg) {
                                var lastVal = boolReg.Value;
                                var readout = (await ReadBoolRegister(boolReg)).Register.Value;
                                if (lastVal != readout) {
                                    InvokeRegisterChanged(boolReg);
                                }
                            }
                            if (reg is SRegister stringReg) {
                                var lastVal = stringReg.Value;
                                var readout = (await ReadStringRegister(stringReg)).Register.Value;
                                if (lastVal != readout) {
                                    InvokeRegisterChanged(stringReg);
                                }

                            }

                        }

                        getPLCinfoCycleCount++;

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

            Register toAdd = null;

            //as number registers
            if (_type == RegisterType.DT_short) {
                toAdd = new NRegister<short>(_address, _name);
            }
            if (_type == RegisterType.DT_ushort) {
                toAdd = new NRegister<ushort>(_address, _name);
            }
            if (_type == RegisterType.DDT_int) {
                toAdd = new NRegister<int>(_address, _name);
            }
            if (_type == RegisterType.DDT_uint) {
                toAdd = new NRegister<uint>(_address, _name);
            }
            if (_type == RegisterType.DDT_float) {
                toAdd = new NRegister<float>(_address, _name);
            }

            if(toAdd == null) {
                toAdd = new BRegister(_address, _type, _name);
            }

            Registers.Add(_address, toAdd);

        }

        internal void AddRegister (Type _colType, int _address, RegisterType _type, string _name = null) {

            Register toAdd = null;

            //as number registers
            if (_type == RegisterType.DT_short) {
                toAdd = new NRegister<short>(_address, _name);
            }
            if (_type == RegisterType.DT_ushort) {
                toAdd = new NRegister<ushort>(_address, _name);
            }
            if (_type == RegisterType.DDT_int) {
                toAdd = new NRegister<int>(_address, _name);
            }
            if (_type == RegisterType.DDT_uint) {
                toAdd = new NRegister<uint>(_address, _name);
            }
            if (_type == RegisterType.DDT_float) {
                toAdd = new NRegister<float>(_address, _name);
            }

            if (toAdd == null) {
                toAdd = new BRegister(_address, _type, _name);
            }

            toAdd.collectionType = _colType;
            Registers.Add(_address, toAdd);

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

        internal void AddRegister (Type _colType, SpecialAddress _spAddress, RegisterType _type, string _name = null) {

            var reg = new BRegister(_spAddress, _type, _name);

            reg.collectionType = _colType;

            //as bool registers
            Registers.Add((int)_spAddress, reg);

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
        public void AddRegister<T>(int _address, int _length = 1, string _name = null) {

            Type regType = typeof(T);

            if (regType != typeof(string) && _length != 1) {
                throw new NotSupportedException($"_lenght parameter only allowed for register of type string");
            }

            if (Registers.Any(x => x.Key == _address)) {
                throw new NotSupportedException($"Cannot add a register multiple times, " +
                    $"make sure that all register attributes or AddRegister assignments have different adresses.");
            }

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
            } else if (regType == typeof(TimeSpan)) {
                Registers.Add(_address, new NRegister<TimeSpan>(_address, _name));
            } else if (regType == typeof(bool)) {
                Registers.Add(_address, new BRegister(_address, RegisterType.R, _name));
            } else {
                throw new NotSupportedException($"The type {regType} is not allowed for Registers \n" +
                                                $"Allowed are: short, ushort, int, uint, float and string");
            }

        }

        internal void AddRegister<T> (Type _colType, int _address, int _length = 1, string _name = null, bool _isBitwise = false, Type _enumType = null) {

            Type regType = typeof(T);

            if (regType != typeof(string) && _length != 1) {
                throw new NotSupportedException($"_lenght parameter only allowed for register of type string");
            }

            if (Registers.Any(x => x.Key == _address) && !_isBitwise) {
                throw new NotSupportedException($"Cannot add a register multiple times, " +
                    $"make sure that all register attributes or AddRegister assignments have different adresses.");
            }

            if (Registers.Any(x => x.Key == _address) && _isBitwise) {
                return;
            }

            Register reg = null;

            if (regType == typeof(short)) {
                reg = new NRegister<short>(_address, _name, _isBitwise);
            } else if (regType == typeof(ushort)) {
                reg = new NRegister<ushort>(_address, _name);
            } else if (regType == typeof(int)) {
                reg = new NRegister<int>(_address, _name, _isBitwise, _enumType);
            } else if (regType == typeof(uint)) {
                reg = new NRegister<uint>(_address, _name);
            } else if (regType == typeof(float)) {
                reg = new NRegister<float>(_address, _name);
            } else if (regType == typeof(string)) {
                reg = new SRegister(_address, _length, _name);
            } else if (regType == typeof(TimeSpan)) {
                reg = new NRegister<TimeSpan>(_address, _name);
            } else if (regType == typeof(bool)) {
                reg = new BRegister(_address, RegisterType.R, _name);
            }


            if (reg == null) {
                throw new NotSupportedException($"The type {regType} is not allowed for Registers \n" +
                                                $"Allowed are: short, ushort, int, uint, float and string");
            } else {

                reg.collectionType = _colType;

                Registers.Add(_address, reg);
            }

        }

        #endregion

        #region Register accessing

        /// <summary>
        /// Gets a register that was added by its name
        /// </summary>
        /// <returns></returns>
        public Register GetRegister (string name) {

            return Registers.FirstOrDefault(x => x.Value.Name == name).Value;

        }

        /// <summary>
        /// Gets a register that was added by its name
        /// </summary>
        /// <typeparam name="T">The type of register</typeparam>
        /// <returns>A casted register or the <code>default</code> value</returns>
        public T GetRegister<T> (string name) where T : Register  {
            try {
                var reg = Registers.FirstOrDefault(x => x.Value.Name == name);
                return reg.Value as T;
            } catch (InvalidCastException) {
                return default(T);
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
