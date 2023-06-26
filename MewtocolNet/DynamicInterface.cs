using MewtocolNet.Exceptions;
using MewtocolNet.Logging;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MewtocolNet
{

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

                        if(reg.IsAllowedRegisterGenericType()) {

                            var lastVal = reg.Value;

                            var rwReg = (IRegisterInternal)reg;

                            var readout = await rwReg.ReadAsync(this);

                            if (lastVal != readout) {

                                rwReg.SetValueFromPLC(readout);
                                InvokeRegisterChanged(reg);
                           
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

            _ = SetRegisterAsync(GetRegister(propName), value);

        }

        #endregion

        #region Register Colleciton adding

        #region Register Collection

        /// <summary>
        /// Attaches a register collection object to 
        /// the interface that can be updated automatically.
        /// <para/>
        /// Just create a class inheriting from <see cref="RegisterCollectionBase"/>
        /// and assert some propertys with the custom <see cref="RegisterAttribute"/>.
        /// </summary>
        /// <param name="collection">A collection inherting the <see cref="RegisterCollectionBase"/> class</param>
        public MewtocolInterface WithRegisterCollection(RegisterCollectionBase collection) {

            collection.PLCInterface = this;

            var props = collection.GetType().GetProperties();

            foreach (var prop in props) {

                var attributes = prop.GetCustomAttributes(true);

                string propName = prop.Name;
                foreach (var attr in attributes) {

                    if (attr is RegisterAttribute cAttribute && prop.PropertyType.IsAllowedPlcCastingType()) {

                        var dotnetType = prop.PropertyType;

                        AddRegister(new RegisterBuildInfo {
                            memoryAddress = cAttribute.MemoryArea,
                            specialAddress = cAttribute.SpecialAddress,
                            memorySizeBytes = cAttribute.ByteLength,
                            registerType = cAttribute.RegisterType,
                            dotnetCastType = dotnetType,
                            collectionType = collection.GetType(),
                            name = prop.Name,
                        });

                    }

                }

            }

            RegisterChanged += (reg) => {

                //register is used bitwise
                if (reg.IsUsedBitwise()) {

                    for (int i = 0; i < props.Length; i++) {

                        var prop = props[i];
                        var bitWiseFound = prop.GetCustomAttributes(true)
                        .FirstOrDefault(y => y.GetType() == typeof(RegisterAttribute) && ((RegisterAttribute)y).MemoryArea == reg.MemoryAddress);

                        if (bitWiseFound != null) {

                            var casted = (RegisterAttribute)bitWiseFound;
                            var bitIndex = casted.AssignedBitIndex;

                            BitArray bitAr = null;

                            if (reg is NumberRegister<short> reg16) {
                                var bytes = BitConverter.GetBytes((short)reg16.Value);
                                bitAr = new BitArray(bytes);
                            } else if (reg is NumberRegister<int> reg32) {
                                var bytes = BitConverter.GetBytes((int)reg32.Value);
                                bitAr = new BitArray(bytes);
                            }

                            if (bitAr != null && bitIndex < bitAr.Length && bitIndex >= 0) {

                                //set the specific bit index if needed
                                prop.SetValue(collection, bitAr[bitIndex]);
                                collection.TriggerPropertyChanged(prop.Name);

                            } else if (bitAr != null) {

                                //set the specific bit array if needed
                                prop.SetValue(collection, bitAr);
                                collection.TriggerPropertyChanged(prop.Name);

                            }

                        }

                    }

                }

                //updating normal properties
                var foundToUpdate = props.FirstOrDefault(x => x.Name == reg.Name);

                if (foundToUpdate != null) {

                    var foundAttributes = foundToUpdate.GetCustomAttributes(true);
                    var foundAttr = foundAttributes.FirstOrDefault(x => x.GetType() == typeof(RegisterAttribute));

                    if (foundAttr == null)
                        return;

                    var registerAttr = (RegisterAttribute)foundAttr;

                    //check if bit parse mode
                    if (registerAttr.AssignedBitIndex == -1) {

                        HashSet<Type> NumericTypes = new HashSet<Type> {
                            typeof(bool),
                            typeof(short),
                            typeof(ushort),
                            typeof(int),
                            typeof(uint),
                            typeof(float),
                            typeof(TimeSpan),
                            typeof(string)
                        };

                        var regValue = ((IRegister)reg).Value;

                        if (NumericTypes.Any(x => foundToUpdate.PropertyType == x)) {
                            foundToUpdate.SetValue(collection, regValue);
                        }

                        if (foundToUpdate.PropertyType.IsEnum) {
                            foundToUpdate.SetValue(collection, regValue);
                        }

                    }

                    collection.TriggerPropertyChanged(foundToUpdate.Name);

                }

            };

            if (collection != null)
                collection.OnInterfaceLinked(this);

            Connected += (i) => {
                if (collection != null)
                    collection.OnInterfaceLinkedAndOnline(this);
            };

            return this;

        }

        #endregion

        #endregion

        #region Register Adding

        internal void AddRegister (RegisterBuildInfo buildInfo) {

            var builtRegister = buildInfo.Build();

            //is bitwise and the register list already contains that area register
            if(builtRegister.IsUsedBitwise() && CheckDuplicateRegister(builtRegister, out var existing)) {

                return;

            }

            if (CheckDuplicateRegister(builtRegister))
                throw MewtocolException.DupeRegister(builtRegister);

            if(CheckDuplicateNameRegister(builtRegister))
                throw MewtocolException.DupeNameRegister(builtRegister);

            Registers.Add(builtRegister);

        }

        public void AddRegister(IRegister register) {

            if (CheckDuplicateRegister(register))
                throw MewtocolException.DupeRegister(register);

            if (CheckDuplicateNameRegister(register))
                throw MewtocolException.DupeNameRegister(register);

            Registers.Add(register);

        }

        private bool CheckDuplicateRegister (IRegister instance, out IRegister foundDupe) {

            foundDupe = Registers.FirstOrDefault(x => x.CompareIsDuplicate(instance));

            return Registers.Contains(instance) || foundDupe != null;

        }

        private bool CheckDuplicateRegister(IRegister instance) {

            var foundDupe = Registers.FirstOrDefault(x => x.CompareIsDuplicate(instance));

            return Registers.Contains(instance) || foundDupe != null;

        }

        private bool CheckDuplicateNameRegister(IRegister instance) {

            return Registers.Any(x => x.CompareIsNameDuplicate(instance));

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
