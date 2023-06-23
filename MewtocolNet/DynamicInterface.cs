using MewtocolNet.Logging;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MewtocolNet {

    /// <summary>
    /// The PLC com interface class
    /// </summary>
    public partial class MewtocolInterface {

        /// <summary>
        /// True if the auto poller is currently paused
        /// </summary>
        public bool PollingPaused => pollerIsPaused;

        /// <summary>
        /// True if the poller is actvice (can be paused)
        /// </summary>
        public bool PollerActive => !pollerTaskStopped;

        internal event Action PolledCycle;

        internal volatile bool pollerTaskRunning;
        internal volatile bool pollerTaskStopped;
        internal volatile bool pollerIsPaused;
        internal volatile bool pollerFirstCycle = false;

        internal bool usePoller = false;

        #region Register Polling

        /// <summary>
        /// Kills the poller completely
        /// </summary>
        internal void KillPoller() {

            pollerTaskRunning = false;
            pollerTaskStopped = true;

            ClearRegisterVals();

        }

        /// <summary>
        /// Pauses the polling and waits for the last message to be sent
        /// </summary>
        /// <returns></returns>
        public async Task PausePollingAsync() {

            if (!pollerTaskRunning)
                return;

            pollerTaskRunning = false;

            while (!pollerIsPaused) {

                if (pollerIsPaused)
                    break;

                await Task.Delay(10);

            }

            pollerTaskRunning = false;

        }

        /// <summary>
        /// Resumes the polling
        /// </summary>
        public void ResumePolling() {

            pollerTaskRunning = true;

        }

        /// <summary>
        /// Attaches a continous reader that reads back the Registers and Contacts
        /// </summary>
        internal void AttachPoller() {

            if (pollerTaskRunning)
                return;

            pollerFirstCycle = true;

            Task.Factory.StartNew(async () => {

                Logger.Log("Poller is attaching", LogLevel.Info, this);

                int iteration = 0;

                pollerTaskStopped = false;
                pollerTaskRunning = true;
                pollerIsPaused = false;

                while (!pollerTaskStopped) {

                    while (pollerTaskRunning) {

                        if (iteration >= Registers.Count + 1) {
                            iteration = 0;
                            //invoke cycle polled event
                            InvokePolledCycleDone();
                            continue;
                        }

                        if (iteration >= Registers.Count) {
                            await GetPLCInfoAsync();
                            iteration++;
                            continue;
                        }

                        var reg = Registers[iteration];

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

                        iteration++;
                        pollerFirstCycle = false;

                        await Task.Delay(pollerDelayMs);

                    }

                    pollerIsPaused = !pollerTaskRunning;

                }

                pollerIsPaused = false;

            });

        }

        internal void PropertyRegisterWasSet(string propName, object value) {

            SetRegister(propName, value);

        }

        #endregion

        #region Register Adding

        //Internal register adding for auto register collection building
        internal void AddRegister<T>(Type _colType, int _address, PropertyInfo boundProp, int _length = 1, bool _isBitwise = false, Type _enumType = null) {

            Type regType = typeof(T);

            if (regType != typeof(string) && _length != 1) {
                throw new NotSupportedException($"_lenght parameter only allowed for register of type string");
            }

            if (Registers.Any(x => x.MemoryAddress == _address) && _isBitwise) {
                return;
            }

            IRegister reg = null;

            string propName = boundProp.Name;

            //rename the property name to prevent duplicate names in case of a bitwise prop
            if (_isBitwise && regType == typeof(short))
                propName = $"Auto_Bitwise_DT{_address}";

            if (_isBitwise && regType == typeof(int))
                propName = $"Auto_Bitwise_DDT{_address}";

            if (regType == typeof(short)) {
                reg = new NRegister<short>(_address, propName, _isBitwise, _enumType).WithCollectionType(_colType);
            } else if (regType == typeof(ushort)) {
                reg = new NRegister<ushort>(_address, propName).WithCollectionType(_colType);
            } else if (regType == typeof(int)) {
                reg = new NRegister<int>(_address, propName, _isBitwise, _enumType).WithCollectionType(_colType);
            } else if (regType == typeof(uint)) {
                reg = new NRegister<uint>(_address, propName).WithCollectionType(_colType);
            } else if (regType == typeof(float)) {
                reg = new NRegister<float>(_address, propName).WithCollectionType(_colType);
            } else if (regType == typeof(string)) {
                reg = new SRegister(_address, _length, propName).WithCollectionType(_colType);
            } else if (regType == typeof(TimeSpan)) {
                reg = new NRegister<TimeSpan>(_address, propName).WithCollectionType(_colType);
            } else if (regType == typeof(bool)) {
                reg = new BRegister(IOType.R, 0x0, _address, propName).WithCollectionType(_colType);
            }

            if (reg == null) {
                throw new NotSupportedException($"The type {regType} is not allowed for Registers \n" +
                                                $"Allowed are: short, ushort, int, uint, float and string");
            }

            if (Registers.Any(x => x.GetRegisterPLCName() == reg.GetRegisterPLCName()) && !_isBitwise) {
                throw new NotSupportedException($"Cannot add a register multiple times, " +
                    $"make sure that all register attributes or AddRegister assignments have different adresses.");
            }

            Registers.Add(reg);

        }

        #endregion

        #region Register accessing

        /// <summary>
        /// Gets a register that was added by its name
        /// </summary>
        /// <returns></returns>
        public IRegister GetRegister(string name) {

            return Registers.FirstOrDefault(x => x.Name == name);

        }

        /// <summary>
        /// Gets a register that was added by its name
        /// </summary>
        /// <typeparam name="T">The type of register</typeparam>
        /// <returns>A casted register or the <code>default</code> value</returns>
        public T GetRegister<T>(string name) where T : IRegister {
            try {

                var reg = Registers.FirstOrDefault(x => x.Name == name);
                return (T)reg;

            } catch (InvalidCastException) {

                return default(T);

            }

        }

        #endregion

        #region Register Reading

        /// <summary>
        /// Gets a list of all added registers
        /// </summary>
        public List<IRegister> GetAllRegisters() {

            return Registers;

        }

        #endregion

        #region Event Invoking 

        internal void InvokeRegisterChanged(IRegister reg) {

            RegisterChanged?.Invoke(reg);

        }

        internal void InvokePolledCycleDone() {

            PolledCycle?.Invoke();

        }

        #endregion

    }

}
