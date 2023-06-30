using MewtocolNet.Logging;
using MewtocolNet.Queue;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MewtocolNet {

    public partial class MewtocolInterface : IPlc, INotifyPropertyChanged, IDisposable {

        #region Private fields

        private protected Stream stream;

        private int tcpMessagesSentThisCycle = 0;
        private int pollerCycleDurationMs;
        private volatile int queuedMessages;
        private bool isConnected;
        private PLCInfo plcInfo;
        private protected int stationNumber;

        private protected int bytesTotalCountedUpstream = 0;
        private protected int bytesTotalCountedDownstream = 0;
        private protected int cycleTimeMs = 25;
        private protected int bytesPerSecondUpstream = 0;
        private protected int bytesPerSecondDownstream = 0;

        private protected AsyncQueue queue = new AsyncQueue();
        private protected int RecBufferSize = 128;
        private protected Stopwatch speedStopwatchUpstr;
        private protected Stopwatch speedStopwatchDownstr;
        private protected Task firstPollTask = new Task(() => { });

        #endregion

        #region Internal fields 

        internal event Action PolledCycle;
        internal volatile bool pollerTaskStopped = true;
        internal volatile bool pollerFirstCycle;
        internal bool usePoller = false;

        internal List<BaseRegister> RegistersUnderlying { get; private set; } = new List<BaseRegister>();
        internal IEnumerable<IRegisterInternal> RegistersInternal => RegistersUnderlying.Cast<IRegisterInternal>();

        #endregion

        #region Public Read Only Properties / Fields

        /// <inheritdoc/>
        public event Action<PLCInfo> Connected;

        /// <inheritdoc/>
        public event Action Disconnected;

        /// <inheritdoc/>
        public event Action<IRegister> RegisterChanged;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public bool Disposed { get; private set; }

        /// <inheritdoc/>
        public int QueuedMessages => queuedMessages;

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
        public IEnumerable<IRegister> Registers => RegistersUnderlying.Cast<IRegister>();

        /// <inheritdoc/>
        public int StationNumber => stationNumber;

        /// <inheritdoc/>
        public int BytesPerSecondUpstream {
            get { return bytesPerSecondUpstream; }
            private protected set {
                bytesPerSecondUpstream = value;
                OnPropChange();
            }
        }

        /// <inheritdoc/>
        public int BytesPerSecondDownstream {
            get { return bytesPerSecondDownstream; }
            private protected set {
                bytesPerSecondDownstream = value;
                OnPropChange();
            }
        }

        #endregion

        #region Public read/write Properties / Fields

        /// <inheritdoc/>
        public int ConnectTimeout { get; set; } = 3000;

        #endregion

        #region Methods

        private protected MewtocolInterface () {

            Connected += MewtocolInterface_Connected;

            void MewtocolInterface_Connected(PLCInfo obj) {

                if (usePoller)
                    AttachPoller();

                IsConnected = true;

            }

            RegisterChanged += (o) => {

                var asInternal = (IRegisterInternal)o;

                string address = $"{asInternal.GetRegisterString()}{asInternal.GetStartingMemoryArea()}".PadRight(5, (char)32);

                Logger.Log($"{address} " +
                           $"{(o.Name != null ? $"({o.Name}) " : "")}" +
                           $"changed to \"{asInternal.GetValueString()}\"", LogLevel.Change, this);
            };

        }
        
        /// <inheritdoc/>
        public virtual Task ConnectAsync () => throw new NotImplementedException();

        /// <inheritdoc/>
        public async Task AwaitFirstDataAsync() => await firstPollTask;

        /// <inheritdoc/>
        public void Disconnect() {

            if (!IsConnected) return;

            pollCycleTask.Wait();

            OnMajorSocketExceptionWhileConnected();

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

        /// <inheritdoc/>
        public async Task<MewtocolFrameResponse> SendCommandAsync(string _msg, bool withTerminator = true, int timeoutMs = -1) {

            //send request
            queuedMessages++;

            var tempResponse = queue.Enqueue(async () => await SendFrameAsync(_msg, withTerminator, withTerminator));

            if (await Task.WhenAny(tempResponse, Task.Delay(timeoutMs)) != tempResponse) {
                // timeout logic
                return new MewtocolFrameResponse(403, "Timed out");
            } 

            tcpMessagesSentThisCycle++;
            queuedMessages--;

            return tempResponse.Result;

        }

        private protected async Task<MewtocolFrameResponse> SendFrameAsync (string frame, bool useBcc = true, bool useCr = true) {

            try {

                if (useBcc)
                    frame = $"{frame.BuildBCCFrame()}";

                if (useCr)
                    frame = $"{frame}\r";

                //write inital command
                byte[] writeBuffer = Encoding.UTF8.GetBytes(frame);
                stream.Write(writeBuffer, 0, writeBuffer.Length);

                Logger.Log($"[---------CMD START--------]", LogLevel.Critical, this);
                Logger.Log($"--> OUT MSG: {frame.Replace("\r", "(CR)")}", LogLevel.Critical, this);

                var readResult = await ReadCommandAsync();

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
                    return new MewtocolFrameResponse(gotErrorcode);
                }

                //was multiframed response
                if (readResult.Item2) {

                    var split = resString.Split('&');

                    for (int j = 0; j < split.Length; j++) {

                        split[j] = split[j].Replace("\r", "");
                        split[j] = split[j].Substring(0, split[j].Length - 2);
                        if (j > 0) split[j] = split[j].Replace($"%{GetStationNumber()}", "");

                    }

                    resString = string.Join("", split);

                }

                Logger.Log($"<-- IN MSG: {resString.Replace("\r", "(CR)")}", LogLevel.Critical, this);
                Logger.Log($"Total bytes parsed: {resString.Length}", LogLevel.Critical, this);
                Logger.Log($"[---------CMD END----------]", LogLevel.Critical, this);

                return new MewtocolFrameResponse(resString);

            } catch (Exception ex) {

                return new MewtocolFrameResponse(400, ex.Message.ToString(System.Globalization.CultureInfo.InvariantCulture));

            }

        }

        private protected async Task<(byte[], bool)> ReadCommandAsync () {

            //read total
            List<byte> totalResponse = new List<byte>();
            bool wasMultiFramedResponse = false;

            try {

                bool needsRead = false;

                do {

                    byte[] buffer = new byte[128];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    byte[] received = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, received, 0, bytesRead);

                    var commandRes = ParseBufferFrame(received);
                    needsRead = commandRes == CommandState.LineFeed || commandRes == CommandState.RequestedNextFrame;

                    var tempMsg = Encoding.UTF8.GetString(received).Replace("\r", "(CR)");
                    Logger.Log($">> IN PART: {tempMsg}, Command state: {commandRes}", LogLevel.Critical, this);

                    //add complete response to collector without empty bytes
                    totalResponse.AddRange(received.Where(x => x != (byte)0x0));

                    if (commandRes == CommandState.RequestedNextFrame) {

                        //request next frame
                        var writeBuffer = Encoding.UTF8.GetBytes("%01**&\r");
                        await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length);
                        wasMultiFramedResponse = true;

                    }

                } while (needsRead);

            } catch (OperationCanceledException) { }

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

        private protected int CheckForErrorMsg (string msg) {

            //error catching
            Regex errorcheck = new Regex(@"\%[0-9]{2}\!([0-9]{2})", RegexOptions.IgnoreCase);
            Match m = errorcheck.Match(msg);

            if (m.Success) {

                string eCode = m.Groups[1].Value;
                return Convert.ToInt32(eCode);

            }

            return 0;

        }

        private protected void OnMajorSocketExceptionWhileConnecting() {

            if (IsConnected) {

                Logger.Log("The PLC connection timed out", LogLevel.Error, this);
                OnDisconnect();

            }

        }

        private protected void OnMajorSocketExceptionWhileConnected() {

            if (IsConnected) {

                Logger.Log("The PLC connection was closed", LogLevel.Error, this);
                OnDisconnect();

            }

        }

        private protected virtual void OnConnected (PLCInfo plcinf) {

            Logger.Log("Connected", LogLevel.Info, this);
            Logger.Log($"\n\n{plcinf.ToString()}\n\n", LogLevel.Verbose, this);

            IsConnected = true;

            Connected?.Invoke(plcinf);

            if (!usePoller) {
                firstPollTask.RunSynchronously();
            }

            PolledCycle += OnPollCycleDone;
            void OnPollCycleDone() {

                firstPollTask.RunSynchronously();
                PolledCycle -= OnPollCycleDone;

            }

        }

        private protected virtual void OnDisconnect () {

            BytesPerSecondDownstream = 0;
            BytesPerSecondUpstream = 0;
            CycleTimeMs = 0;

            IsConnected = false;
            ClearRegisterVals();

            Disconnected?.Invoke();
            KillPoller();

        }

        private protected void ClearRegisterVals() {

            for (int i = 0; i < RegistersUnderlying.Count; i++) {

                var reg = (IRegisterInternal)RegistersUnderlying[i];
                reg.ClearValue();

            }

        }

        private protected void OnPropChange([CallerMemberName] string propertyName = null) {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        #endregion

    }

}
