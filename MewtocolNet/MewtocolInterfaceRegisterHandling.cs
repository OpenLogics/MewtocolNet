using MewtocolNet.Exceptions;
using MewtocolNet.Logging;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.Registers;
using MewtocolNet.UnderlyingRegisters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public abstract partial class MewtocolInterface {

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

        private List<RegisterCollection> registerCollections = new List<RegisterCollection>();

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
        public async Task<int> RunPollerCylceManual() {

            if (!pollerTaskStopped)
                throw new NotSupportedException($"The poller is already running, " +
                $"please make sure there is no polling active before calling {nameof(RunPollerCylceManual)}");

            tcpMessagesSentThisCycle = 0;

            pollCycleTask = OnMultiFrameCycle();
            await pollCycleTask;

            return tcpMessagesSentThisCycle;

        }

        //polls all registers one by one (slow)
        internal async Task Poll() {

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

        private async Task OnMultiFrameCycle() {

            var sw = Stopwatch.StartNew();

            //await UpdateRCPRegisters();

            //await UpdateDTRegisters();

            await memoryManager.PollAllAreasAsync();

            await GetPLCInfoAsync();

            sw.Stop();
            PollerCycleDurationMs = (int)sw.ElapsedMilliseconds;

        }

        #endregion

        #region Smart register polling methods

        private async Task UpdateRCPRegisters() {

            //build booleans
            var rcpList = RegistersUnderlying.Where(x => x.GetType() == typeof(BoolRegister))
                                   .Select(x => (BoolRegister)x)
                                   .ToArray();

            //one frame can only read 8 registers at a time
            int rcpFrameCount = (int)Math.Ceiling((double)rcpList.Length / 8);
            int rcpLastFrameRemainder = rcpList.Length <= 8 ? rcpList.Length : rcpList.Length % 8;

            for (int i = 0; i < rcpFrameCount; i++) {

                int toReadRegistersCount = 8;

                if (i == rcpFrameCount - 1) toReadRegistersCount = rcpLastFrameRemainder;

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

                    if ((bool)register.Value != resultBitArray[k]) {
                        register.SetValueFromPLC(resultBitArray[k]);
                    }

                }

            }

        }

        private async Task UpdateDTRegisters() {

            foreach (var reg in RegistersUnderlying) {

                var type = reg.GetType();

                if (reg.RegisterType.IsNumericDTDDT() || reg.RegisterType == RegisterType.DT_BYTE_RANGE) {

                    var lastVal = reg.Value;
                    var rwReg = (IRegisterInternal)reg;
                    var readout = await rwReg.ReadAsync();
                    if (readout == null) return;

                    if (lastVal != readout) {
                        rwReg.SetValueFromPLC(readout);
                    }

                }

            }

        }

        #endregion

        #region Register Colleciton adding

        /// <summary>
        /// Adds the given register collection and all its registers with attributes to the register list
        /// </summary>
        internal void WithRegisterCollections(List<RegisterCollection> collections) {

            if (registerCollections.Count != 0)
                throw new NotSupportedException("Register collections can only be build once");

            List<RegisterBuildInfo> buildInfos = new List<RegisterBuildInfo>();

            foreach (var collection in collections) {

                collection.PLCInterface = this;

                var props = collection.GetType().GetProperties();

                foreach (var prop in props) {

                    var attributes = prop.GetCustomAttributes(true);

                    string propName = prop.Name;
                    foreach (var attr in attributes) {

                        if (attr is RegisterAttribute cAttribute) {

                            if (!prop.PropertyType.IsAllowedPlcCastingType()) {
                                throw new MewtocolException($"The register attribute property type is not allowed ({prop.PropertyType})");
                            }

                            var dotnetType = prop.PropertyType;

                            buildInfos.Add(new RegisterBuildInfo {
                                mewAddress = cAttribute.MewAddress,
                                dotnetCastType = dotnetType.IsEnum ? dotnetType.UnderlyingSystemType : dotnetType,
                                collectionTarget = collection,
                                boundPropTarget = prop,
                            });

                        }

                    }

                }

                if (collection != null) {
                    registerCollections.Add(collection);
                    collection.OnInterfaceLinked(this);
                }

                Connected += (i) => {
                    if (collection != null)
                        collection.OnInterfaceLinkedAndOnline(this);
                };

            }

            AddRegisters(buildInfos);

        }

        /// <summary>
        /// Writes back the values changes of the underlying registers to the corrosponding property
        /// </summary>
        private void OnRegisterChangedUpdateProps(IRegisterInternal reg) {

            var collection = reg.ContainedCollection;
            if (collection == null) return;

            var props = collection.GetType().GetProperties();

            //set the specific bit array if needed
            //prop.SetValue(collection, bitAr);
            //collection.TriggerPropertyChanged(prop.Name);



        }

        #endregion

        #region Register Adding

        /// <inheritdoc/>
        public void AddRegister (IRegister register) => AddRegister(register as BaseRegister);

        /// <inheritdoc/>
        public void AddRegister (BaseRegister register) {

            if (CheckDuplicateRegister(register))
                throw MewtocolException.DupeRegister(register);

            if (CheckDuplicateNameRegister(register))
                throw MewtocolException.DupeNameRegister(register);

            if (CheckOverlappingRegister(register, out var regB))
                throw MewtocolException.OverlappingRegister(register, regB);

            register.attachedInterface = this;
            RegistersUnderlying.Add(register);

        }

        // Used for internal property based register building
        internal void AddRegisters (List<RegisterBuildInfo> buildInfos) {

            //build all from attribute
            List<BaseRegister> registers = new List<BaseRegister>();

            foreach (var buildInfo in buildInfos) {

                var builtRegister = buildInfo.BuildForCollectionAttribute();

                int? linkLen = null;

                if(builtRegister is BytesRegister bReg) {

                    linkLen = (int?)bReg.ReservedBytesSize ?? bReg.ReservedBitSize;

                }

                //attach the property and collection
                builtRegister.WithBoundProperty(new RegisterPropTarget {
                    BoundProperty = buildInfo.boundPropTarget,
                    LinkLength = linkLen,
                });

                builtRegister.WithRegisterCollection(buildInfo.collectionTarget);

                builtRegister.attachedInterface = this;
                registers.Add(builtRegister);

            }

            //order by address
            registers = registers.OrderBy(x => x.GetSpecialAddress()).ToList();
            registers = registers.OrderBy(x => x.MemoryAddress).ToList();

            //link to memory manager
            for (int i = 0, j = 0; i < registers.Count; i++) {

                BaseRegister reg = registers[i];
                reg.name = $"auto_prop_register_{j + 1}";

                //link the memory area to the register
                if (memoryManager.LinkRegister(reg)) {

                    RegistersUnderlying.Add(reg);
                    j++;

                }

            }

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

        private bool CheckOverlappingRegister (IRegisterInternal instance, out IRegisterInternal regB) {

            //ignore bool registers, they have their own address spectrum
            regB = null;
            if (instance is BoolRegister) return false;

            uint addressFrom = instance.MemoryAddress;
            uint addressTo = addressFrom + instance.GetRegisterAddressLen();

            var foundOverlapping = RegistersInternal.FirstOrDefault(x => {

                //ignore bool registers, they have their own address spectrum
                if (x is BoolRegister) return false;

                uint addressF = x.MemoryAddress;
                uint addressT = addressF + x.GetRegisterAddressLen();

                bool matchingBaseAddress = addressFrom < addressT && addressF < addressTo;

                return matchingBaseAddress;

            });

            if (foundOverlapping != null) {
                regB = foundOverlapping;
                return true;
            }

            return false;

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

        private protected void ClearRegisterVals() {

            for (int i = 0; i < RegistersUnderlying.Count; i++) {

                var reg = (IRegisterInternal)RegistersUnderlying[i];
                reg.ClearValue();

            }

        }

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
