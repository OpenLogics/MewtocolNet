using MewtocolNet.Exceptions;
using MewtocolNet.Logging;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet {

    /// <summary>
    /// The PLC com interface class
    /// </summary>
    public partial class MewtocolInterface {

        internal Task pollCycleTask;

        /// <summary>
        /// True if the poller is actvice (can be paused)
        /// </summary>
        public bool PollerActive => !pollerTaskStopped;

        /// <summary>
        /// Current poller cycle duration
        /// </summary>
        public int PollerCycleDurationMs { 
            get => pollerCycleDurationMs; 
            private set {
                pollerCycleDurationMs = value;
                OnPropChange();
            }
        }

        #region Register Polling

        /// <summary>
        /// Kills the poller completely
        /// </summary>
        internal void KillPoller() {

            pollerTaskStopped = true;
            ClearRegisterVals();

        }

        /// <summary>
        /// Attaches a continous reader that reads back the Registers and Contacts
        /// </summary>
        internal void AttachPoller() {

            if (!pollerTaskStopped)
                return;

            PollerCycleDurationMs = 0;
            pollerFirstCycle = true;

            Task.Run(Poll);

        }

        /// <summary>
        /// Runs a single poller cycle manually,
        /// useful if you want to use a custom update frequency
        /// </summary>
        /// <returns>The number of inidvidual mewtocol commands sent</returns>
        public async Task<int> RunPollerCylceManual () {

            if (!pollerTaskStopped)
                throw new NotSupportedException($"The poller is already running, " +
                $"please make sure there is no polling active before calling {nameof(RunPollerCylceManual)}");

            tcpMessagesSentThisCycle = 0;

            pollCycleTask = OnMultiFrameCycle();
            await pollCycleTask;

            return tcpMessagesSentThisCycle;

        }

        //polls all registers one by one (slow)
        internal async Task Poll () {

            Logger.Log("Poller is attaching", LogLevel.Info, this);

            pollerTaskStopped = false;

            while (!pollerTaskStopped) {

                tcpMessagesSentThisCycle = 0;

                pollCycleTask = OnMultiFrameCycle();
                await pollCycleTask;

                if (!IsConnected) {
                    pollerTaskStopped = true;
                    return;
                }

                pollerFirstCycle = false;
                InvokePolledCycleDone();

            }

        }

        private async Task OnMultiFrameCycle () {

            var sw = Stopwatch.StartNew();

            await UpdateRCPRegisters();

            await UpdateDTRegisters();

            await GetPLCInfoAsync();

            sw.Stop();
            PollerCycleDurationMs = (int)sw.ElapsedMilliseconds;

        }

        #endregion

        #region Smart register polling methods

        private async Task UpdateRCPRegisters () {

            //build booleans
            var rcpList = RegistersUnderlying.Where(x => x.GetType() == typeof(BoolRegister))
                                   .Select(x => (BoolRegister)x)
                                   .ToArray();

            //one frame can only read 8 registers at a time
            int rcpFrameCount = (int)Math.Ceiling((double)rcpList.Length / 8);
            int rcpLastFrameRemainder = rcpList.Length <= 8 ? rcpList.Length : rcpList.Length % 8;

            for (int i = 0; i < rcpFrameCount; i++) {

                int toReadRegistersCount = 8;

                if(i == rcpFrameCount - 1) toReadRegistersCount = rcpLastFrameRemainder;

                var rcpString = new StringBuilder($"%{GetStationNumber()}#RCP{toReadRegistersCount}");

                for (int j = 0; j < toReadRegistersCount; j++) {

                    BoolRegister register = rcpList[i + j];
                    rcpString.Append(register.BuildMewtocolQuery());

                }

                string rcpRequest = rcpString.ToString();
                var result = await SendCommandAsync(rcpRequest);
                if (!result.Success) return;

                var resultBitArray = result.Response.ParseRCMultiBit();

                for (int k = 0; k < resultBitArray.Length; k++) {

                    var register = rcpList[i + k];

                    if((bool)register.Value != resultBitArray[k]) {
                        register.SetValueFromPLC(resultBitArray[k]);
                        InvokeRegisterChanged(register);
                    }

                }
                    
            }

        }

        private async Task UpdateDTRegisters () {

            foreach (var reg in RegistersUnderlying) {

                var type = reg.GetType();

                if(reg.RegisterType.IsNumericDTDDT() || reg.RegisterType == RegisterType.DT_BYTE_RANGE) {

                    var lastVal = reg.Value;
                    var rwReg = (IRegisterInternal)reg;
                    var readout = await rwReg.ReadAsync();
                    if (readout == null) return;

                    if (lastVal != readout) {
                        rwReg.SetValueFromPLC(readout);
                        InvokeRegisterChanged(reg);
                    }

                }

            }

        }

        #endregion

        #region Register Colleciton adding

        internal MewtocolInterface WithRegisterCollection (RegisterCollection collection) {

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
                if (reg.GetType() == typeof(BytesRegister)) {

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

        #region Register Adding

        /// <inheritdoc/>
        public void AddRegister(BaseRegister register) {

            if (CheckDuplicateRegister(register))
                throw MewtocolException.DupeRegister(register);

            if (CheckDuplicateNameRegister(register))
                throw MewtocolException.DupeNameRegister(register);

            register.attachedInterface = this;
            RegistersUnderlying.Add(register);

        }

        /// <inheritdoc/>
        public void AddRegister(IRegister register) => AddRegister(register as BaseRegister);

        internal void AddRegister (RegisterBuildInfo buildInfo) {

            var builtRegister = buildInfo.Build();

            //is bitwise and the register list already contains that area register
            if(builtRegister.GetType() == typeof(BytesRegister) && CheckDuplicateRegister(builtRegister, out var existing)) {

                return;

            }

            if (CheckDuplicateRegister(builtRegister))
                throw MewtocolException.DupeRegister(builtRegister);

            if(CheckDuplicateNameRegister(builtRegister))
                throw MewtocolException.DupeNameRegister(builtRegister);

            builtRegister.attachedInterface = this;
            RegistersUnderlying.Add(builtRegister);

        }

        private bool CheckDuplicateRegister (IRegisterInternal instance, out IRegisterInternal foundDupe) {

            foundDupe = RegistersInternal.FirstOrDefault(x => x.CompareIsDuplicate(instance));

            return RegistersInternal.Contains(instance) || foundDupe != null;

        }

        private bool CheckDuplicateRegister(IRegisterInternal instance) {

            var foundDupe = RegistersInternal.FirstOrDefault(x => x.CompareIsDuplicate(instance));

            return RegistersInternal.Contains(instance) || foundDupe != null;

        }

        private bool CheckDuplicateNameRegister(IRegisterInternal instance) {

            return RegistersInternal.Any(x => x.CompareIsNameDuplicate(instance));

        }

        #endregion

        #region Register accessing

        /// <inheritdoc/>>
        public IRegister GetRegister(string name) {

            return RegistersUnderlying.FirstOrDefault(x => x.Name == name);

        }

        /// <inheritdoc/>
        public IEnumerable<IRegister> GetAllRegisters () {

            return RegistersUnderlying.Cast<IRegister>();

        }

        #endregion

        #region Event Invoking 

        internal void PropertyRegisterWasSet(string propName, object value) {

            _ = SetRegisterAsync(GetRegister(propName), value);

        }

        internal void InvokeRegisterChanged(IRegister reg) {

            RegisterChanged?.Invoke(reg);

        }

        internal void InvokePolledCycleDone() {

            PolledCycle?.Invoke();

        }

        #endregion

    }

}
