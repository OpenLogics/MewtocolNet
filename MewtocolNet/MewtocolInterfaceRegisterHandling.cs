﻿using MewtocolNet.Events;
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
        private bool heartbeatTimerRunning = false;

        private protected Task firstPollTask;

        internal Task heartbeatTask = Task.CompletedTask;

        internal bool disableHeartbeat = false;
        internal Func<IPlc, Task> heartbeatCallbackTask;
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
                heartbeatTimerRunning = false;

            }
            
        }

        private void TestPollerStartNeeded () {

            if (!IsConnected) return;

            if(!heartbeatTimerRunning) {
                heartBeatTimer = new System.Timers.Timer();
                heartBeatTimer.Interval = 3000;
                heartBeatTimer.Elapsed += PollTimerTick;
                heartBeatTimer.Start();
                heartbeatTimerRunning = true;
            }
            
            if (!usePoller || PollerActive) return;

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

            if (disableHeartbeat) {

                await Task.CompletedTask;
                return;

            }

            if (regularSendTask != null && !regularSendTask.IsCompleted) await regularSendTask;

            Logger.LogVerbose("Sending heartbeat", this);

            if (await GetInfoAsync() == null) {

                Logger.LogError("Heartbeat timed out", this);

                OnSocketExceptionWhileConnected();

                return;

            }

            if(heartbeatCallbackTask != null && (plcInfo.IsRunMode || execHeartBeatCallbackTaskInProg)) 
                await heartbeatCallbackTask(this);

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

            var sw = Stopwatch.StartNew();

            await memoryManager.PollAllAreasAsync(async () => {

                //await the timed task before starting a new poller cycle
                if (heartbeatNeedsRun || pollerFirstCycle) {

                    await HeartbeatTickTask();

                    heartbeatNeedsRun = false;

                }

            });

            sw.Stop();

            if (firstPollTask != null && !firstPollTask.IsCompleted) {

                firstPollTask.RunSynchronously();
                firstPollTask = null;
                Logger.Log("poll cycle first done");
            
            }

            pollerFirstCycleCompleted = true;
            PollerCycleDurationMs = (int)sw.ElapsedMilliseconds;

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
                            .AddressFromAttribute(cAttribute.MewAddress, cAttribute.TypeDef, collection, prop, cAttribute, byteHint)
                            .AsType(dotnetType.IsEnum ? dotnetType.UnderlyingSystemType : dotnetType)
                            .PollLevel(pollLevel)
                            .Finalize();

                        }

                    }

                }

                if (collection != null) {
                    registerCollections.Add(collection);
                    collection.OnInterfaceLinked(this);
                }

            }

            AddRegisters(regBuild.assembler.assembled.ToArray());

        }

        #endregion

        #region Register Adding

        /// <inheritdoc/>>
        public void BuildRegisters (Action<RBuildMulti> builder) {

            var regBuilder = new RBuildMulti(this);

            builder.Invoke(regBuilder);

            this.AddRegisters(regBuilder.assembler.assembled.ToArray());

        }

        /// <inheritdoc/>>
        public void ClearAllRegisters () {

            memoryManager.ClearAllRegisters();

        }

        internal void AddRegisters(params Register[] registers) {

            memoryManager.LinkAndMergeRegisters(registers.ToList());

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
