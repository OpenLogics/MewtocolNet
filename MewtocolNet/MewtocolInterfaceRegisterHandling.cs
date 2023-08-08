using MewtocolNet.Events;
using MewtocolNet.Logging;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.RegisterBuilding.BuilderPatterns;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MewtocolNet {

    /// <summary>
    /// The PLC com interface class
    /// </summary>
    public abstract partial class MewtocolInterface {

        private bool heartbeatNeedsRun = false;

        internal Task heartbeatTask = Task.CompletedTask;

        internal Func<Task> heartbeatCallbackTask;
        internal bool execHeartBeatCallbackTaskInProg = false;

        internal Task pollCycleTask;

        private List<RegisterCollection> registerCollections = new List<RegisterCollection>();

        internal IEnumerable<Register> RegistersInternal => GetAllRegistersInternal();

        public IEnumerable<IRegister> Registers => GetAllRegisters();

        /// <summary>
        /// True if the poller is actvice (can be paused)
        /// </summary>
        public bool PollerActive => !pollerTaskStopped;

        /// <inheritdoc/>
        public int PollerCycleDurationMs {
            get => pollerCycleDurationMs;
            private set {
                pollerCycleDurationMs = value;
                OnPropChange();
            }
        }

        private System.Timers.Timer heartBeatTimer;

        internal volatile bool pollerFirstCycleCompleted;

        #region Register Polling

        internal void WatchPollerDemand() {

            memoryManager.MemoryLayoutChanged += () => TestPollerStartNeeded();

            Connected += (s, e) => TestPollerStartNeeded();
            Reconnected += (s, e) => TestPollerStartNeeded();

            Disconnected += (s, e) => {

                StopHeartBeat();
            
            };

        }

        private void StopHeartBeat () {

            if(heartBeatTimer != null) {

                heartBeatTimer.Elapsed -= PollTimerTick;
                heartBeatTimer.Dispose();

            }
            
        }

        private void TestPollerStartNeeded () {

            if (!IsConnected) return;

            heartBeatTimer = new System.Timers.Timer();
            heartBeatTimer.Interval = 3000;
            heartBeatTimer.Elapsed += PollTimerTick;
            heartBeatTimer.Start();

            if (!usePoller) return;

            bool hasCyclic = memoryManager.HasCyclicPollableRegisters();
            bool hasFirstCycle = memoryManager.HasSingleCyclePollableRegisters();

            if (hasCyclic || hasFirstCycle) AttachPoller();

        }

        /// <summary>
        /// Kills the poller completely
        /// </summary>
        internal void KillPoller() {

            pollerFirstCycleCompleted = false;
            pollerTaskStopped = true;

        }

        /// <summary>
        /// Attaches a continous reader that reads back the Registers and Contacts
        /// </summary>
        internal void AttachPoller() {

            if (!pollerTaskStopped) return;

            pollerFirstCycleCompleted = false;
            PollerCycleDurationMs = 0;
            pollerFirstCycle = true;

            Task.Run(Poll);

        }

        private void PollTimerTick(object sender, System.Timers.ElapsedEventArgs e) {

            if(!IsConnected || isConnectingStage || isReconnectingStage) return;

            heartBeatTimer.Stop();

            heartbeatNeedsRun = true;

            if(!PollerActive) {

                Task.Run(HeartbeatTickTask);

            }

        }

        private async Task HeartbeatTickTask () {

            if (regularSendTask != null && !regularSendTask.IsCompleted) await regularSendTask;

            Logger.LogVerbose("Sending heartbeat", this);

            if (await GetInfoAsync() == null) {

                Logger.LogError("Heartbeat timed out", this);

                OnSocketExceptionWhileConnected();

                return;

            }

            if(heartbeatCallbackTask != null && (plcInfo.IsRunMode || execHeartBeatCallbackTaskInProg)) 
                await heartbeatCallbackTask();

            Logger.LogVerbose("End heartbeat", this);

            heartBeatTimer.Start();

        }

        /// <summary>
        /// Runs a single poller cycle manually,
        /// useful if you want to use a custom update frequency
        /// </summary>
        /// <returns>The number of inidvidual mewtocol commands sent</returns>
        public async Task<int> UpdateAsync() {

            if (!pollerTaskStopped)
                throw new NotSupportedException($"The poller is already running, " +
                $"please make sure there is no polling active before calling {nameof(UpdateAsync)}");

            tcpMessagesSentThisCycle = 0;

            pollCycleTask = OnMultiFrameCycle();
            await pollCycleTask;

            if (!memoryManager.HasCyclicPollableRegisters()) KillPoller();

            return tcpMessagesSentThisCycle;

        }

        //performs one poll cycle, one cycle is defined as getting all regster values
        //and (not every cycle) the status of the plc that is performed on a timer basis
        internal async Task Poll() {

            Logger.Log("Poller is attaching", this);

            pollerTaskStopped = false;

            while (!pollerTaskStopped) {

                tcpMessagesSentThisCycle = 0;

                pollCycleTask = OnMultiFrameCycle();
                await pollCycleTask;

                if (!memoryManager.HasCyclicPollableRegisters()) KillPoller();

                InvokePolledCycleDone();

                if (!IsConnected) {
                    pollerTaskStopped = true;
                    return;
                }

                pollerFirstCycle = false;
                
            }

        }

        private async Task OnMultiFrameCycle() {

            //await the timed task before starting a new poller cycle
            if (heartbeatNeedsRun) {

                await HeartbeatTickTask();
                
                heartbeatNeedsRun = false;
            
            }

            var sw = Stopwatch.StartNew();

            await memoryManager.PollAllAreasAsync(async () => {

                await RunOneOpenQueuedTask();

            });

            sw.Stop();

            pollerFirstCycleCompleted = true;
            PollerCycleDurationMs = (int)sw.ElapsedMilliseconds;

        }

        #endregion

        #region Smart register polling methods

        [Obsolete]
        private async Task UpdateRCPRegisters() {

            //build booleans
            //var rcpList = RegistersUnderlying.Where(x => x.GetType() == typeof(BoolRegister))
            //              .Select(x => (BoolRegister)x)
            //              .ToArray();

            ////one frame can only read 8 registers at a time
            //int rcpFrameCount = (int)Math.Ceiling((double)rcpList.Length / 8);
            //int rcpLastFrameRemainder = rcpList.Length <= 8 ? rcpList.Length : rcpList.Length % 8;

            //for (int i = 0; i < rcpFrameCount; i++) {

            //    int toReadRegistersCount = 8;

            //    if (i == rcpFrameCount - 1) toReadRegistersCount = rcpLastFrameRemainder;

            //    var rcpString = new StringBuilder($"%{GetStationNumber()}#RCP{toReadRegistersCount}");

            //    for (int j = 0; j < toReadRegistersCount; j++) {

            //        BoolRegister register = rcpList[i + j];
            //        rcpString.Append(register.BuildMewtocolQuery());

            //    }

            //    string rcpRequest = rcpString.ToString();
            //    var result = await SendCommandAsync(rcpRequest);
            //    if (!result.Success) return;

            //    var resultBitArray = result.Response.ParseRCMultiBit();

            //    for (int k = 0; k < resultBitArray.Length; k++) {

            //        var register = rcpList[i + k];

            //        if ((bool)register.Value != resultBitArray[k]) {
            //            register.SetValueFromPLC(resultBitArray[k]);
            //        }

            //    }

            //}

        }

        #endregion

        #region Register Collection adding

        /// <summary>
        /// Adds the given register collection and all its registers with attributes to the register list
        /// </summary>
        internal void WithRegisterCollections(List<RegisterCollection> collections) {

            if (registerCollections.Count != 0)
                throw new NotSupportedException("Register collections can only be build once");

            var regBuild = new RBuildFromAttributes(this);

            foreach (var collection in collections) {

                collection.PLCInterface = this;

                var props = collection.GetType().GetProperties();

                foreach (var prop in props) {

                    var attributes = prop.GetCustomAttributes(true);

                    string propName = prop.Name;
                    foreach (var attr in attributes) {

                        if (attr is RegisterAttribute cAttribute) {

                            var pollFreqAttr = (PollLevelAttribute)attributes.FirstOrDefault(x => x.GetType() == typeof(PollLevelAttribute));
                            var stringHintAttr = (StringHintAttribute)attributes.FirstOrDefault(x => x.GetType() == typeof(StringHintAttribute));

                            var dotnetType = prop.PropertyType;
                            int pollLevel = 1;
                            uint? byteHint = (uint?)stringHintAttr?.size;

                            if (pollFreqAttr != null) pollLevel = pollFreqAttr.pollLevel;

                            //add builder item
                            regBuild
                            .AddressFromAttribute(cAttribute.MewAddress, cAttribute.TypeDef, collection, prop, byteHint)
                            .AsType(dotnetType.IsEnum ? dotnetType.UnderlyingSystemType : dotnetType)
                            .PollLevel(pollLevel);

                        }

                    }

                }

                if (collection != null) {
                    registerCollections.Add(collection);
                    collection.OnInterfaceLinked(this);
                }

                Connected += (s,e) => {
                    if (collection != null)
                        collection.OnInterfaceLinkedAndOnline(this);
                };

            }

            var assembler = new RegisterAssembler(this);

            AddRegisters(assembler.assembled.ToArray());

        }

        /// <summary>
        /// Writes back the values changes of the underlying registers to the corrosponding property
        /// </summary>
        private void OnRegisterChangedUpdateProps(Register reg) {

            var collection = reg.ContainedCollection;
            if (collection == null) return;

            var props = collection.GetType().GetProperties();

            //set the specific bit array if needed
            //prop.SetValue(collection, bitAr);
            //collection.TriggerPropertyChanged(prop.Name);



        }

        #endregion

        #region Register Adding

        internal void AddRegisters(params Register[] registers) {

            InsertRegistersToMemoryStack(registers.ToList());

        }

        internal void InsertRegistersToMemoryStack(List<Register> registers) {

            memoryManager.LinkAndMergeRegisters(registers);

            //run a second iteration
            //memoryManager.LinkAndMergeRegisters();

        }

        private bool CheckDuplicateRegister(Register instance, out Register foundDupe) {

            foundDupe = RegistersInternal.FirstOrDefault(x => x.CompareIsDuplicate(instance));

            return RegistersInternal.Contains(instance) || foundDupe != null;

        }

        private bool CheckDuplicateRegister(Register instance) {

            var foundDupe = RegistersInternal.FirstOrDefault(x => x.CompareIsDuplicate(instance));

            return RegistersInternal.Contains(instance) || foundDupe != null;

        }

        private bool CheckDuplicateNameRegister(Register instance) {

            return RegistersInternal.Any(x => x.CompareIsNameDuplicate(instance));

        }

        private bool CheckOverlappingRegister(Register instance, out Register regB) {

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

            return RegistersInternal.FirstOrDefault(x => x.Name == name);

        }

        /// <inheritdoc/>
        public IEnumerable<IRegister> GetAllRegisters() {

            return memoryManager.GetAllRegisters().Cast<IRegister>();

        }

        internal IEnumerable<Register> GetAllRegistersInternal() {

            return memoryManager.GetAllRegisters();

        }

        #endregion

        #region Event Invoking 

        private protected void ClearRegisterVals() {

            var internals = RegistersInternal.ToList();

            for (int i = 0; i < internals.Count; i++) {

                var reg = internals[i];
                reg.ClearValue();
                //reg.TriggerNotifyChange();

            }

        }

        internal void PropertyRegisterWasSet(string propName, object value) {

            throw new NotImplementedException();

        }

        internal void InvokeRegisterChanged(Register reg, object preValue, string preValueString) {

            RegisterChanged?.Invoke(this, new RegisterChangedArgs {
                Register = reg,
                PreviousValue = preValue,
                PreviousValueString = preValueString,
                Value = reg.ValueObj,
            });

        }

        internal void InvokePolledCycleDone() {

            PolledCycle?.Invoke();

        }

        #endregion

    }

}
