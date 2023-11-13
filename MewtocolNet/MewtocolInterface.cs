using MewtocolNet.Events;
using MewtocolNet.Helpers;
using MewtocolNet.Logging;
using MewtocolNet.RegisterBuilding.BuilderPatterns;
using MewtocolNet.Registers;
using MewtocolNet.UnderlyingRegisters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet {

    public abstract partial class MewtocolInterface : IPlc {

        #region Events

        /// <inheritdoc/>
        public event PlcConnectionEventHandler Connected;

        /// <inheritdoc/>
        public event PlcConnectionEventHandler Reconnected;

        /// <inheritdoc/>
        public event PlcReconnectEventHandler ReconnectTryStarted;

        /// <inheritdoc/>
        public event PlcConnectionEventHandler Disconnected;

        /// <inheritdoc/>
        public event RegisterChangedEventHandler RegisterChanged;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public event PlcModeChangedEventHandler ModeChanged;

        #endregion

        #region Private fields

        //thread locker for messages
        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        //cancellation token for the messages
        internal CancellationTokenSource tSourceMessageCancel = new CancellationTokenSource();
        internal CancellationTokenSource tSourceReconnecting;

        private protected Stream stream;

        private int tcpMessagesSentThisCycle = 0;
        private int pollerCycleDurationMs;
        private bool isConnected;
        private PLCInfo plcInfo;
        private protected int stationNumber;

        private protected int RecBufferSize = 128;
        private protected int bytesTotalCountedUpstream = 0;
        private protected int bytesTotalCountedDownstream = 0;
        private protected int cycleTimeMs = 25;
        private protected int bytesPerSecondUpstream = 0;
        private protected int bytesPerSecondDownstream = 0;

        private protected Stopwatch speedStopwatchUpstr;
        private protected Stopwatch speedStopwatchDownstr;

        private protected Task reconnectTask;
        private protected Task<MewtocolFrameResponse> regularSendTask;

        private protected bool wasInitialStatusReceived;
        private protected bool supportsExtendedMessageHeader;
        private protected string messageHeader = "%";
        private protected MewtocolVersion mewtocolVersion;

        #endregion

        #region Internal fields 

        internal protected System.Timers.Timer cyclicGenericUpdateCounter;
        internal event Action PolledCycle;

        internal volatile bool pollerTaskStopped = true;
        internal volatile bool pollerFirstCycle;
        internal MemoryAreaManager memoryManager;

        //configuration

        private volatile protected bool isMessageLocked;
        private volatile bool isReceiving;
        private volatile bool isSending;

        internal int sendReceiveTimeoutMs = 1000;
        internal int heartbeatIntervalMs = 3000;
        internal int tryReconnectAttempts = 0;
        internal int tryReconnectDelayMs = 1000;

        internal bool usePoller = false;
        internal bool alwaysGetMetadata = false;

        internal Func<int, Task> onBeforeReconnectTryTask;

        #endregion

        #region Public Read Only Properties / Fields

        /// <inheritdoc/>
        public int QueuedMessages => semaphoreSlim.CurrentCount;

        /// <inheritdoc/>
        public bool Disposed { get; private set; }

        /// <inheritdoc/>
        public bool IsConnected {
            get => isConnected;
            private protected set {
                isConnected = value;
                OnPropChange();
            }
        }

        /// <inheritdoc/>
        public PLCInfo PlcInfo {
            get => plcInfo;
            private set {
                plcInfo = value;
                OnPropChange();
            }
        }

        /// <inheritdoc/>
        public int StationNumber => stationNumber;

        /// <inheritdoc/>
        public bool IsSending {
            get => isSending;
            private set {
                isSending = value;
                OnPropChange();
            }
        }

        /// <inheritdoc/>
        public int BytesPerSecondUpstream => bytesPerSecondUpstream;

        /// <inheritdoc/>
        public bool IsReceiving { 
            get => isReceiving; 
            private set {
                isReceiving = value;
                OnPropChange();
            }
        }

        /// <inheritdoc/>
        public int BytesPerSecondDownstream => bytesPerSecondDownstream;

        /// <inheritdoc/>
        public bool IsRunMode => PlcInfo.IsRunMode;

        /// <inheritdoc/>
        public MewtocolVersion MewtocolVersion {
            get => mewtocolVersion;
            private protected set {
                mewtocolVersion = value;
                OnPropChange();
            }
        }

        /// <inheritdoc/>
        public string ConnectionInfo => GetConnectionInfo();

        /// <inheritdoc/>
        public RBuildAnon Register => new RBuildAnon(this);

        #endregion

        #region Public read/write Properties / Fields

        /// <inheritdoc/>
        public int ConnectTimeout { get; set; } = 3000;

        #endregion
        
        #region Methods

        private protected MewtocolInterface() {

            Logger.Start();

            memoryManager = new MemoryAreaManager(this);

            WatchPollerDemand();

            Connected += MewtocolInterface_Connected;
            Disconnected += MewtocolInterface_Disconnected;
            RegisterChanged += OnRegisterChanged;

            PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(PlcInfo) && PlcInfo != null) {
                    PlcInfo.PropertyChanged += (s1, e1) => {
                        if (e1.PropertyName == nameof(PlcInfo.IsRunMode)) 
                            OnPropChange(nameof(IsRunMode));
                    };
                }
            };

            memoryManager.MemoryLayoutChanged += () => {

                OnPropChange(nameof(MemoryAreas));
                OnPropChange(nameof(Registers));

            };

        }

        internal MewtocolInterface Build () {

            memoryManager.LinkAndMergeRegisters();

            return this;

        }

        private void MewtocolInterface_Connected(object sender, PlcConnectionArgs args) {

            IsConnected = true;

        }

        private void MewtocolInterface_Disconnected(object sender, PlcConnectionArgs e) {

            Logger.LogVerbose("Disconnected", this);

        }

        private void OnRegisterChanged(object sender, RegisterChangedArgs args) {

            var asInternal = (Register)args.Register;

            //log
            if(IsConnected) {

                var sb = new StringBuilder();

                sb.Append(asInternal.GetMewName());
                if (asInternal.Name != null) {
                    sb.Append(asInternal.autoGenerated ? $" (Auto)" : $" ({asInternal.Name})");
                }
                sb.Append($" {asInternal.underlyingSystemType.Name}");
                sb.Append($" changed \"{args.PreviousValueString.Ellipsis(25)}\"" +
                          $" => \"{asInternal.GetValueString().Ellipsis(75)}\"");

                Logger.Log(sb.ToString(), LogLevel.Change, this);

            }

        }

        /// <inheritdoc/>
        public virtual async Task<ConnectResult> ConnectAsync(Func<Task> callBack = null) {

            isConnectingStage = false;

            await memoryManager.OnPlcConnected();

            Logger.Log($"PLC: {PlcInfo.TypeName}", LogLevel.Verbose, this);
            Logger.Log($"TYPE CODE: {PlcInfo.TypeCode.ToString("X")}", LogLevel.Verbose, this);
            Logger.Log($"OP MODE: {PlcInfo.OperationMode}", LogLevel.Verbose, this);
            Logger.Log($"PROG CAP: {PlcInfo.ProgramCapacity}k", LogLevel.Verbose, this);
            Logger.Log($"HW INFO: {PlcInfo.HardwareInformation}", LogLevel.Verbose, this);
            Logger.Log($"DIAG ERR: {PlcInfo.SelfDiagnosticError}", LogLevel.Verbose, this);
            Logger.Log($"CPU VER: {PlcInfo.CpuVersion}", LogLevel.Verbose, this);

            if(alwaysGetMetadata && PlcInfo.Metadata != null) {

                Logger.LogVerbose($"METADATA: {PlcInfo.Metadata.MetaDataVersion}", this);
                Logger.LogVerbose($"FP-WIN VERSION: {PlcInfo.Metadata.FPWinVersion}", this);
                Logger.LogVerbose($"Project VERSION: {PlcInfo.Metadata.ProjectVersion}", this);
                Logger.LogVerbose($"Company ID: {PlcInfo.Metadata.CompanyID}", this);
                Logger.LogVerbose($"Application ID: {PlcInfo.Metadata.ApplicationID}", this);
                Logger.LogVerbose($"Project ID: {PlcInfo.Metadata.ProjectID}", this);
                Logger.LogVerbose($"LAST CONF CHNG: {PlcInfo.Metadata.LastConfigChangeDate}", this);
                Logger.LogVerbose($"LAST POU CHNG: {PlcInfo.Metadata.LastPouChangeDate}", this);
                Logger.LogVerbose($"LAST USR-LIB CHNG: {PlcInfo.Metadata.LastUserLibChangeDate}", this);

            }

            Logger.Log($">> Intial connection end <<", LogLevel.Verbose, this);

            if (callBack != null) {

                await callBack();

                Logger.Log($">> OnConnected run complete <<", LogLevel.Verbose, this);

            }

            //run all register collection on online tasks
            foreach (var col in registerCollections) {

                await col.OnInterfaceLinkedAndOnline(this);

            }

            Logger.Log($">> OnConnected register collections run complete <<", LogLevel.Verbose, this);

            return ConnectResult.Unknown;

        }

        /// <inheritdoc/>
        protected virtual Task ReconnectAsync(int conTimeout, CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <inheritdoc/>
        public async Task AwaitFirstDataCycleAsync() {

            if(firstPollTask != null && !firstPollTask.IsCompleted && IsConnected)
                await firstPollTask;

            await Task.CompletedTask;

        }

        /// <inheritdoc/>
        public async Task DisconnectAsync() {

            if (pollCycleTask != null) await pollCycleTask;

            Disconnect();

        }

        /// <inheritdoc/>
        public void Disconnect() {

            if (!IsConnected) return;

            if (pollCycleTask != null && !pollCycleTask.IsCompleted) pollCycleTask.Wait();

            if (IsConnected) {

                tSourceMessageCancel.Cancel();
                isMessageLocked = false;

                Logger.Log("The PLC connection was closed manually", LogLevel.Error, this);
                OnDisconnect();

            }

        }

        /// <inheritdoc/>
        public void Dispose() {

            if (Disposed) return;

            Disconnect();

            //GC.SuppressFinalize(this);
            Disposed = true;

        }

        /// <inheritdoc/>
        public virtual string GetConnectionInfo() => throw new NotImplementedException();

        #region Reconnecting

        /// <inheritdoc/>
        public void WithReconnectTask(Func<int, Task> callback) {

            onBeforeReconnectTryTask = callback;

        }

        internal void StartReconnectTask() {

            if (reconnectTask == null) {

                tSourceReconnecting = new CancellationTokenSource();

                reconnectTask = Task.Run(async () => {

                    int retryCount = 1;

                    if (!IsConnected) {

                        if(this is MewtocolInterfaceTcp tcpI) {

                            tcpI.client.Close();
                            tcpI.client = null;

                        }

                        while (!IsConnected && tryReconnectAttempts > 0 && retryCount < tryReconnectAttempts + 1 && !tSourceReconnecting.Token.IsCancellationRequested) {

                            if (tSourceReconnecting.Token.IsCancellationRequested) break;

                            if(onBeforeReconnectTryTask != null)
                                await onBeforeReconnectTryTask(retryCount);

                            Logger.Log($"Reconnecting {retryCount}/{tryReconnectAttempts} ...", this);

                            //kill the poller
                            KillPoller();

                            //stop the heartbeat timer for the time of retries
                            StopHeartBeat();

                            var eArgs = new ReconnectArgs(retryCount, tryReconnectAttempts, TimeSpan.FromMilliseconds(tryReconnectDelayMs + ConnectTimeout));
                            ReconnectTryStarted?.Invoke(this, eArgs);

                            Reconnected += (s, e) => eArgs.ConnectionSuccess();

                            await ReconnectAsync(tryReconnectDelayMs, tSourceReconnecting.Token);

                            if (tSourceReconnecting.Token.IsCancellationRequested) break;

                            if (IsConnected) return;

                            await Task.Delay(tryReconnectDelayMs);

                            retryCount++;


                        }

                        //still not connected
                        if (!IsConnected) {

                            //invoke the dc evnt
                            OnMajorSocketExceptionAfterRetries();

                        }

                    }

                });

            }

        }

        /// <inheritdoc/>
        public void StopReconnecting () {

            if (tSourceReconnecting != null && !tSourceReconnecting.Token.IsCancellationRequested)
                tSourceReconnecting.Cancel();

        }

        #endregion

        #region Message sending and queuing

        //internally used send task
        internal async Task<MewtocolFrameResponse> SendCommandInternalAsync(string _msg, Action<double> onReceiveProgress = null, int? overrideTimeout = null) {

            if (tSourceMessageCancel.Token.IsCancellationRequested) return MewtocolFrameResponse.Canceled;

            if (!IsConnected && !isConnectingStage && !isReconnectingStage)
                throw new NotSupportedException("The device must be connected to send a message");

            //thread lock the current cycle
            await semaphoreSlim.WaitAsync();

            isMessageLocked = true;

            MewtocolFrameResponse responseData;

            try {

                //send request
                regularSendTask = SendTwoDirectionalFrameAsync(_msg, onReceiveProgress);

                var timeoutAwaiter = await Task.WhenAny(regularSendTask, Task.Delay(overrideTimeout ?? sendReceiveTimeoutMs, tSourceMessageCancel.Token));

                if (timeoutAwaiter != regularSendTask) {

                    isMessageLocked = false;
                    regularSendTask = null;

                    // timeout logic
                    return MewtocolFrameResponse.Timeout;

                }

                //canceled
                if (regularSendTask.IsCanceled) {

                    isMessageLocked = false;
                    regularSendTask = null;

                    return MewtocolFrameResponse.Canceled;

                }

                responseData = regularSendTask.Result;

                tcpMessagesSentThisCycle++;

            } catch (OperationCanceledException) {

                return MewtocolFrameResponse.Canceled;

            } finally {

                //unlock
                semaphoreSlim.Release();

                OnPropChange(nameof(QueuedMessages));

            }

            isMessageLocked = false;
            regularSendTask = null;

            return responseData;

        }

        private protected async Task<MewtocolFrameResponse> SendTwoDirectionalFrameAsync(string frame, Action<double> onReceiveProgress = null) {

            try {

                if (stream == null) return MewtocolFrameResponse.NotIntialized;

                frame = $"{frame.BCC_Mew()}\r";

                SetUpstreamStopWatchStart();

                IsSending = true;

                if (tSourceMessageCancel.Token.IsCancellationRequested) return MewtocolFrameResponse.Canceled;

                //write inital command
                byte[] writeBuffer = Encoding.UTF8.GetBytes(frame);
                await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length, tSourceMessageCancel.Token);

                IsSending = false;

                //calculate the expected number of frames from the message request
                int? wordsCountRequested = null;
                if (onReceiveProgress != null) {

                    var match = Regex.Match(frame, @"RDD(?<from>[0-9]{5})(?<to>[0-9]{5})");

                    if (match.Success) {
                        var from = int.Parse(match.Groups["from"].Value);
                        var to = int.Parse(match.Groups["to"].Value);
                        wordsCountRequested = (to - from) + 1;
                    }

                }

                //calc upstream speed
                CalcUpstreamSpeed(writeBuffer.Length);

                OnOutMsg(frame);

                var readResult = await ReadCommandAsync(wordsCountRequested, onReceiveProgress);

                //did not receive bytes but no errors, the com port was not configured right
                if (readResult.Item1.Length == 0) {

                    return new MewtocolFrameResponse(402, "Receive buffer was empty");

                }

                //build final result
                string resString = Encoding.UTF8.GetString(readResult.Item1);

                //check if the message had errors
                //error response
                var gotErrorcode = CheckForErrorMsg(resString);
                if (gotErrorcode != 0) {
                    var errResponse = new MewtocolFrameResponse(gotErrorcode);
                    Logger.Log($"Command error: {errResponse.Error}", LogLevel.Error, this);
                    return errResponse;
                }

                //was multiframed response
                if (readResult.Item2) {

                    var split = resString.Split('&');

                    for (int j = 0; j < split.Length; j++) {

                        split[j] = split[j].Replace("\r", "");

                        if (j < split.Length - 1) {
                            //on last frame include csum
                            split[j] = split[j].Substring(0, split[j].Length - 2);

                        }

                        if (j > 0) 
                            split[j] = split[j]
                            .Replace($"%{GetStationNumber()}", "")
                            .Replace($"<{GetStationNumber()}", "");

                    }

                    resString = string.Join("", split);

                }

                OnInMsg(resString);

                return new MewtocolFrameResponse(resString);

            } catch (OperationCanceledException) {

                return MewtocolFrameResponse.Canceled;
            
            } catch (Exception ex) {

                return new MewtocolFrameResponse(400, ex.Message.ToString(System.Globalization.CultureInfo.InvariantCulture));

            }

        }

        private protected async Task<(byte[], bool)> ReadCommandAsync(int? wordsCountRequested = null, Action<double> onReceiveProgress = null) {

            //read total
            List<byte> totalResponse = new List<byte>();
            bool wasMultiFramedResponse = false;

            try {

                bool needsRead = false;
                int readFrames = 0;
                int readBytesPayload = 0;

                do {

                    if (onReceiveProgress != null && wordsCountRequested != null) onReceiveProgress(0);

                    SetDownstreamStopWatchStart();

                    byte[] buffer = new byte[RecBufferSize];
                    IsReceiving = true;

                    if (tSourceMessageCancel.Token.IsCancellationRequested) break;

                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, tSourceMessageCancel.Token);
                    IsReceiving = false;

                    CalcDownstreamSpeed(bytesRead);

                    byte[] received = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, received, 0, bytesRead);

                    var commandRes = ParseBufferFrame(received);
                    needsRead = commandRes == CommandState.LineFeed || commandRes == CommandState.RequestedNextFrame;

                    var tempMsgStr = Encoding.UTF8.GetString(received);

                    OnInMsgPart(tempMsgStr);

                    //add complete response to collector without empty bytes
                    totalResponse.AddRange(received.Where(x => x != (byte)0x0));

                    if (commandRes == CommandState.RequestedNextFrame) {

                        string cmdPrefix = tempMsgStr.StartsWith("<") ? "<" : "%";

                        //request next frame
                        var writeBuffer = Encoding.UTF8.GetBytes($"{cmdPrefix}{GetStationNumber()}**&\r");

                        IsSending = true;
                        await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length, tSourceMessageCancel.Token);
                        IsSending = false;
                        Logger.Log($">> Requested next frame", LogLevel.Critical, this);
                        wasMultiFramedResponse = true;

                    }

                    //calc frame progress
                    if (onReceiveProgress != null && wordsCountRequested != null) {

                        if (readFrames == 0) {
                            readBytesPayload += received.Length - 9;
                        } else {
                            readBytesPayload += received.Length - 7;
                        }

                        var frameBytesPlayloadCount = readBytesPayload / 2;
                        double prog = (double)frameBytesPlayloadCount / (wordsCountRequested.Value * 2);
                        onReceiveProgress(prog);

                    }

                    readFrames++;

                } while (needsRead);

            } 
            catch (OperationCanceledException) { }
            catch (IOException ex) {

                Logger.LogError($"Socket exception encountered: {ex.Message.ToString()}", this);

                //socket io exception
                OnSocketExceptionWhileConnected();

            }

            return (totalResponse.ToArray(), wasMultiFramedResponse);

        }

        private protected CommandState ParseBufferFrame(byte[] received) {

            const char CR = '\r';
            const char DELIMITER = '&';

            CommandState cmdState;

            bool terminatorReceived = received.Any(x => x == (byte)CR);
            var delimiterTerminatorIdx = received.ToArray().SearchBytePattern(new byte[] { (byte)DELIMITER, (byte)CR });

            if (terminatorReceived && delimiterTerminatorIdx == -1) {
                cmdState = CommandState.Complete;
            } else if (delimiterTerminatorIdx != -1) {
                cmdState = CommandState.RequestedNextFrame;
            } else {
                cmdState = CommandState.LineFeed;
            }

            return cmdState;

        }

        private protected int CheckForErrorMsg(string msg) {

            //error catching
            Regex errorcheck = new Regex(@"...\!([0-9]{2})", RegexOptions.IgnoreCase);
            Match m = errorcheck.Match(msg);

            if (m.Success) {

                string eCode = m.Groups[1].Value;
                return Convert.ToInt32(eCode);

            }

            return 0;

        }

        private protected void OnOutMsg(string outMsg) {
            
            Logger.Log($"[---------CMD START--------]", LogLevel.Critical, this);
            var formatted = $"S -> : {outMsg.Replace("\r", "(CR)")}";
            Logger.Log(formatted, LogLevel.Critical, this);

        }

        private protected void OnInMsgPart(string inPart) {

            var formatted = $"<< IN PART: {inPart.Replace("\r", "(CR)")}";
            Logger.Log(formatted, LogLevel.Critical, this);

        }

        private protected void OnInMsg(string inMsg) {

            var formatted = $"R <- : {inMsg.Replace("\r", "(CR)")}";
            Logger.Log(formatted, LogLevel.Critical, this);
            OnEndMsg();

        }

        private protected void OnEndMsg () {

            Logger.Log($"[---------CMD END----------]", LogLevel.Critical, this);

        }

        private protected void OnMajorSocketExceptionWhileConnecting() {

            if (IsConnected) {

                tSourceMessageCancel.Cancel();
                isMessageLocked = false;

                Logger.Log("The PLC connection timed out", LogLevel.Error, this);
                OnDisconnect();

            }

        }

        private protected void OnSocketExceptionWhileConnected() {

            tSourceMessageCancel.Cancel();

            bytesPerSecondDownstream = 0;
            bytesPerSecondUpstream = 0;

            isMessageLocked = false;
            IsConnected = false;

            if (reconnectTask == null) StartReconnectTask();

        }

        private protected void OnMajorSocketExceptionAfterRetries() {

            Logger.LogError($"Failed to re-connect, closing PLC", this);

            OnDisconnect();

        }

        #endregion

        private protected virtual void OnConnected(PLCInfo plcinf) {

            Logger.Log("Connected to PLC", LogLevel.Info, this);

            //start timer for register update data
            cyclicGenericUpdateCounter = new System.Timers.Timer(1000);
            cyclicGenericUpdateCounter.Elapsed += OnGenericUpdateTimerTick;
            cyclicGenericUpdateCounter.Start();

            //notify the registers
            GetAllRegisters().Cast<Register>().ToList().ForEach(x => x.OnPlcConnected());

            reconnectTask = null;
            IsConnected = true;
            isReconnectingStage = false;
            isConnectingStage = false;

            Connected?.Invoke(this, new PlcConnectionArgs());

        }

        private protected void OnReconnected () {

            IsReceiving = false;
            IsSending = false;
            bytesPerSecondDownstream = 0;
            bytesPerSecondUpstream = 0;
            PollerCycleDurationMs = 0;

            isMessageLocked = false;

            ClearRegisterVals();
            KillPoller();

            //generate a new cancellation token source
            tSourceMessageCancel = new CancellationTokenSource();

            IsConnected = true;
            isReconnectingStage = false;
            isConnectingStage = false;
            reconnectTask = null;

            Reconnected?.Invoke(this, new PlcConnectionArgs());

        }

        private protected virtual void OnDisconnect() {

            IsReceiving = false;
            IsSending = false;
            bytesPerSecondDownstream = 0;
            bytesPerSecondUpstream = 0;
            PollerCycleDurationMs = 0;
            PlcInfo = null;

            isMessageLocked = false;
            IsConnected = false;
            ClearRegisterVals();

            Disconnected?.Invoke(this, new PlcConnectionArgs());
            KillPoller();

            if(cyclicGenericUpdateCounter != null) {

                cyclicGenericUpdateCounter.Elapsed -= OnGenericUpdateTimerTick;
                cyclicGenericUpdateCounter.Dispose();

            }
            
            GetAllRegisters().Cast<Register>().ToList().ForEach(x => x.OnPlcDisconnected());

            //generate a new cancellation token source
            tSourceMessageCancel = new CancellationTokenSource();

        }

        private void OnGenericUpdateTimerTick(object sender, System.Timers.ElapsedEventArgs e) {

            GetAllRegisters().Cast<Register>()
            .ToList().ForEach(x => x.OnInterfaceCyclicTimerUpdate((int)cyclicGenericUpdateCounter.Interval));

            OnPropChange(nameof(BytesPerSecondUpstream));
            OnPropChange(nameof(BytesPerSecondDownstream));

        }

        private void SetUpstreamStopWatchStart() {

            if (speedStopwatchUpstr == null) {
                speedStopwatchUpstr = Stopwatch.StartNew();
            }

            if (speedStopwatchUpstr.Elapsed.TotalSeconds >= 1) {
                speedStopwatchUpstr.Restart();
                bytesTotalCountedUpstream = 0;
            }

        }

        private void SetDownstreamStopWatchStart() {

            if (speedStopwatchDownstr == null) {
                speedStopwatchDownstr = Stopwatch.StartNew();
            }

            if (speedStopwatchDownstr.Elapsed.TotalSeconds >= 1) {
                speedStopwatchDownstr.Restart();
                bytesTotalCountedDownstream = 0;
            }

        }

        private void CalcUpstreamSpeed(int byteCount) {

            bytesTotalCountedUpstream += byteCount;

            var perSecUpstream = (double)((bytesTotalCountedUpstream / speedStopwatchUpstr.Elapsed.TotalMilliseconds) * 1000);
            if (perSecUpstream <= 10000)
                bytesPerSecondUpstream = (int)Math.Round(perSecUpstream, MidpointRounding.AwayFromZero);

        }

        private void CalcDownstreamSpeed(int byteCount) {

            bytesTotalCountedDownstream += byteCount;

            var perSecDownstream = (double)((bytesTotalCountedDownstream / speedStopwatchDownstr.Elapsed.TotalMilliseconds) * 1000);

            if (perSecDownstream <= 10000)
                bytesPerSecondDownstream = (int)Math.Round(perSecDownstream, MidpointRounding.AwayFromZero);

        }

        private protected void OnPropChange([CallerMemberName] string propertyName = null) {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        internal void InvokeModeChanged(OPMode before, OPMode now) {

            ModeChanged?.Invoke(this, new PlcModeArgs {
                ProgToRun = !before.HasFlag(OPMode.RunMode) && now.HasFlag(OPMode.RunMode),
                RunToProg = before.HasFlag(OPMode.RunMode) && !now.HasFlag(OPMode.RunMode),
                LastMode = before,
                NowMode = now,
            });

        }

        #endregion

        /// <inheritdoc/>
        public string Explain() => memoryManager.ExplainLayout();

        /// <inheritdoc/>
        public IReadOnlyList<IMemoryArea> MemoryAreas => memoryManager.GetAllMemoryAreas();

    }

}
