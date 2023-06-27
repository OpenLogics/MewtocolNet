using MewtocolNet.Exceptions;
using MewtocolNet.Logging;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
        internal bool pollerUseMultiFrame = false;

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

            Task.Run(Poll);

        }

        /// <summary>
        /// Runs a single poller cycle manually,
        /// useful if you want to use a custom update frequency
        /// </summary>
        /// <returns></returns>
        public async Task RunPollerCylceManual (bool useMultiFrame = false) {

            if (useMultiFrame) {
                await OnMultiFrameCycle();
            } else {
                await OnSingleFrameCycle();
            }

        }

        //polls all registers one by one (slow)
        internal async Task Poll () {

            Logger.Log("Poller is attaching", LogLevel.Info, this);

            int iteration = 0;

            pollerTaskStopped = false;
            pollerTaskRunning = true;
            pollerIsPaused = false;

            while (!pollerTaskStopped) {

                if (iteration >= Registers.Count + 1) {
                    iteration = 0;
                    //invoke cycle polled event
                    InvokePolledCycleDone();
                    continue;
                }

                if(pollerUseMultiFrame) {
                    await OnMultiFrameCycle();
                } else {
                    await OnSingleFrameCycle();
                }

                pollerFirstCycle = false;

                iteration++;

                pollerIsPaused = !pollerTaskRunning;

            }

            pollerIsPaused = false;

        }

        private async Task OnSingleFrameCycle () {

            foreach (var reg in Registers) {

                if (reg.IsAllowedRegisterGenericType()) {

                    var lastVal = reg.Value;

                    var rwReg = (IRegisterInternal)reg;

                    var readout = await rwReg.ReadAsync();

                    if (lastVal != readout) {

                        rwReg.SetValueFromPLC(readout);
                        InvokeRegisterChanged(reg);

                    }

                }

            }

            await GetPLCInfoAsync();

        }

        private async Task OnMultiFrameCycle () {

            await UpdateRCPRegisters();

            await GetPLCInfoAsync();

        }

        private async Task UpdateRCPRegisters () {

            //build booleans
            var rcpList = Registers.Where(x => x.GetType() == typeof(BoolRegister))
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

        internal void PropertyRegisterWasSet(string propName, object value) {

            _ = SetRegisterAsync(GetRegister(propName), value);

        }

        #endregion

        #region Register Colleciton adding

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
            Registers.Add(builtRegister);

        }

        public void AddRegister (BaseRegister register) {

            if (CheckDuplicateRegister(register))
                throw MewtocolException.DupeRegister(register);

            if (CheckDuplicateNameRegister(register))
                throw MewtocolException.DupeNameRegister(register);

            register.attachedInterface = this;
            Registers.Add(register);

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

        /// <summary>
        /// Gets a register that was added by its name
        /// </summary>
        /// <returns></returns>
        public IRegister GetRegister(string name) {

            return Registers.FirstOrDefault(x => x.Name == name);

        }

        #endregion

        #region Register Reading

        /// <summary>
        /// Gets a list of all added registers
        /// </summary>
        public IEnumerable<IRegister> GetAllRegisters() {

            return Registers.Cast<IRegister>();

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
