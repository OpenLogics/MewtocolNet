using MewtocolNet.Exceptions;
using MewtocolNet.Logging;
using MewtocolNet.Queue;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MewtocolNet {

    /// <summary>
    /// The PLC com interface class
    /// </summary>
    public partial class MewtocolInterface : INotifyPropertyChanged, IDisposable {

        /// <summary>
        /// Gets triggered when the PLC connection was established
        /// </summary>
        public event Action<PLCInfo> Connected;

        /// <summary>
        /// Gets triggered when the PLC connection was closed or lost
        /// </summary>
        public event Action Disconnected;

        /// <summary>
        /// Gets triggered when a registered data register changes its value
        /// </summary>
        public event Action<IRegister> RegisterChanged;

        /// <summary>
        /// Gets triggered when a property of the interface changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private int connectTimeout = 3000;
        /// <summary>
        /// The initial connection timeout in milliseconds
        /// </summary>
        public int ConnectTimeout {
            get { return connectTimeout; }
            set { connectTimeout = value; }
        }

        private volatile int queuedMessages;
        /// <summary>
        /// Currently queued Messages
        /// </summary>
        public int QueuedMessages {
            get => queuedMessages;
        }

        /// <summary>
        /// The host ip endpoint, leave it null to use an automatic interface
        /// </summary>
        public IPEndPoint HostEndpoint { get; set; }

        private bool isConnected;
        /// <summary>
        /// The current connection state of the interface
        /// </summary>
        public bool IsConnected {
            get => isConnected;
            private set {
                isConnected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
            }
        }

        private bool disposed;
        /// <summary>
        /// True if the current interface was disposed
        /// </summary>
        public bool Disposed {
            get { return disposed; }
            private set { disposed = value; }
        }


        private PLCInfo plcInfo;
        /// <summary>
        /// Generic information about the connected PLC
        /// </summary>
        public PLCInfo PlcInfo {
            get => plcInfo;
            private set {
                plcInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlcInfo)));
            }
        }

        /// <summary>
        /// The registered data registers of the PLC
        /// </summary>
        internal List<BaseRegister> RegistersUnderlying { get; private set; } = new List<BaseRegister>();

        public IEnumerable<IRegister> Registers => RegistersUnderlying.Cast<IRegister>();

        internal IEnumerable<IRegisterInternal> RegistersInternal => RegistersUnderlying.Cast<IRegisterInternal>();

        private string ip;
        private int port;
        private int stationNumber;
        private int cycleTimeMs = 25;

        private int bytesTotalCountedUpstream = 0;
        private int bytesTotalCountedDownstream = 0;

        /// <summary>
        /// The current IP of the PLC connection
        /// </summary>
        public string IpAddress => ip;
        /// <summary>
        /// The current port of the PLC connection
        /// </summary>
        public int Port => port;
        /// <summary>
        /// The station number of the PLC
        /// </summary>
        public int StationNumber => stationNumber;

        /// <summary>
        /// The duration of the last message cycle
        /// </summary>
        public int CycleTimeMs {
            get { return cycleTimeMs; }
            private set {
                cycleTimeMs = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CycleTimeMs)));
            }
        }

        private int bytesPerSecondUpstream = 0;
        /// <summary>
        /// The current transmission speed in bytes per second
        /// </summary>
        public int BytesPerSecondUpstream {
            get { return bytesPerSecondUpstream; }
            private set {
                bytesPerSecondUpstream = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BytesPerSecondUpstream)));
            }
        }

        private int bytesPerSecondDownstream = 0;
        /// <summary>
        /// The current transmission speed in bytes per second
        /// </summary>
        public int BytesPerSecondDownstream {
            get { return bytesPerSecondDownstream; }
            private set {
                bytesPerSecondDownstream = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BytesPerSecondDownstream)));
            }
        }

        internal NetworkStream stream;
        internal TcpClient client;
        internal readonly SerialQueue queue = new SerialQueue();
        private int RecBufferSize = 128;
        internal int SendExceptionsInRow = 0;
        internal bool ImportantTaskRunning = false;

        private Stopwatch speedStopwatchUpstr;
        private Stopwatch speedStopwatchDownstr;

        private Task firstPollTask = new Task(() => { });

        #region Initialization

        /// <summary>
        /// Builds a new Interfacer for a PLC
        /// </summary>
        /// <param name="_ip">IP adress of the PLC</param>
        /// <param name="_port">Port of the PLC</param>
        /// <param name="_station">Station Number of the PLC</param>
        public MewtocolInterface(string _ip, int _port = 9094, int _station = 1) {

            ip = _ip;
            port = _port;
            stationNumber = _station;

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

        #endregion

        #region Setup

        /// <summary>
        /// Trys to connect to the PLC by the IP given in the constructor
        /// </summary>
        /// <param name="OnConnected">
        /// Gets called when a connection with a PLC was established
        /// <para/>
        /// If <see cref="WithPoller"/> is used it waits for the first data receive cycle to complete
        /// </param>
        /// <param name="OnFailed">Gets called when an error or timeout during connection occurs</param>
        /// <returns></returns>
        public async Task<MewtocolInterface> ConnectAsync(Action<PLCInfo> OnConnected = null, Action OnFailed = null) {

            firstPollTask = new Task(() => { });

            Logger.Log("Connecting to PLC...", LogLevel.Info, this);

            var plcinf = await GetPLCInfoAsync();

            if (plcinf != null) {

                Logger.Log("Connected", LogLevel.Info, this);
                Logger.Log($"\n\n{plcinf.ToString()}\n\n", LogLevel.Verbose, this);

                Connected?.Invoke(plcinf);

                if (!usePoller) {
                    if (OnConnected != null) OnConnected(plcinf);
                    firstPollTask.RunSynchronously();
                    return this;
                }

                PolledCycle += OnPollCycleDone;
                void OnPollCycleDone() {

                    if (OnConnected != null) OnConnected(plcinf);
                    firstPollTask.RunSynchronously();
                    PolledCycle -= OnPollCycleDone;

                }

            } else {

                if (OnFailed != null) {
                    OnFailed();
                    Disconnected?.Invoke();
                    firstPollTask.RunSynchronously();
                    Logger.Log("Initial connection failed", LogLevel.Info, this);
                }

            }

            return this;

        }

        /// <summary>
        /// Use this to await the first poll iteration after connecting,
        /// This also completes if the initial connection fails
        /// </summary>
        public async Task AwaitFirstDataAsync () => await firstPollTask;

        /// <summary>
        /// Changes the connections parameters of the PLC, only applyable when the connection is offline
        /// </summary>
        /// <param name="_ip">Ip adress</param>
        /// <param name="_port">Port number</param>
        /// <param name="_station">Station number</param>
        public void ChangeConnectionSettings(string _ip, int _port, int _station = 1) {

            if (IsConnected)
                throw new Exception("Cannot change the connection settings while the PLC is connected");

            ip = _ip;
            port = _port;
            stationNumber = _station;

        }

        /// <summary>
        /// Closes the connection all cyclic polling 
        /// </summary>
        public void Disconnect() {

            if (!IsConnected)
                return;

            OnMajorSocketExceptionWhileConnected();

        }

        /// <summary>
        /// Attaches a poller to the interface that continously 
        /// polls the registered data registers and writes the values to them
        /// </summary>
        public MewtocolInterface WithPoller() {

            usePoller = true;
            return this;

        }

        #endregion

        #region TCP connection state handling

        private async Task ConnectTCP() {

            if (!IPAddress.TryParse(ip, out var targetIP)) {
                throw new ArgumentException("The IP adress of the PLC was no valid format");
            }

            try {

                if (HostEndpoint != null) {

                    client = new TcpClient(HostEndpoint) {
                        ReceiveBufferSize = RecBufferSize,
                        NoDelay = false,
                    };
                    var ep = (IPEndPoint)client.Client.LocalEndPoint;
                    Logger.Log($"Connecting [MAN] endpoint: {ep.Address}:{ep.Port}", LogLevel.Verbose, this);

                } else {

                    client = new TcpClient() {
                        ReceiveBufferSize = RecBufferSize,
                        NoDelay = false,
                        ExclusiveAddressUse = true,
                    };

                }

                var result = client.BeginConnect(targetIP, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(ConnectTimeout));

                if (!success || !client.Connected) {
                    OnMajorSocketExceptionWhileConnecting();
                    return;
                }

                if (HostEndpoint == null) {
                    var ep = (IPEndPoint)client.Client.LocalEndPoint;
                    Logger.Log($"Connecting [AUTO] endpoint: {ep.Address.MapToIPv4()}:{ep.Port}", LogLevel.Verbose, this);
                }

                stream = client.GetStream();
                stream.ReadTimeout = 1000;

                await Task.CompletedTask;

            } catch (SocketException) {

                OnMajorSocketExceptionWhileConnecting();

            }

        }

        #endregion

        #region Low level command handling

        /// <summary>
        /// Calculates the checksum automatically and sends a command to the PLC then awaits results
        /// </summary>
        /// <param name="_msg">MEWTOCOL Formatted request string ex: %01#RT</param>
        /// <param name="withTerminator">Append the checksum and bcc automatically</param>
        /// <returns>Returns the result</returns>
        public async Task<MewtocolFrameResponse> SendCommandAsync(string _msg, bool withTerminator = true) {

            //send request
            queuedMessages++;
            var tempResponse = await queue.Enqueue(() => SendFrameAsync(_msg, withTerminator, withTerminator));

            tcpMessagesSentThisCycle++;
            queuedMessages--;

            return tempResponse;

        }

        private async Task<MewtocolFrameResponse> SendFrameAsync (string frame, bool useBcc = true, bool useCr = true) {

            try {

                //stop time
                if (speedStopwatchUpstr == null) {
                    speedStopwatchUpstr = Stopwatch.StartNew();
                }

                if (speedStopwatchUpstr.Elapsed.TotalSeconds >= 1) {
                    speedStopwatchUpstr.Restart();
                    bytesTotalCountedUpstream = 0;
                }

                const char CR = '\r';
                const char DELIMITER = '&';

                if (client == null || !client.Connected) await ConnectTCP();

                if (useBcc)
                    frame = $"{frame.BuildBCCFrame()}";

                if (useCr)
                    frame = $"{frame}\r";

                //write inital command
                byte[] writeBuffer = Encoding.UTF8.GetBytes(frame);
                await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length);

                //calc upstream speed
                bytesTotalCountedUpstream += writeBuffer.Length;

                var perSecUpstream = (double)((bytesTotalCountedUpstream / speedStopwatchUpstr.Elapsed.TotalMilliseconds) * 1000);
                if (perSecUpstream <= 10000)
                    BytesPerSecondUpstream = (int)Math.Round(perSecUpstream, MidpointRounding.AwayFromZero);


                Logger.Log($"[---------CMD START--------]", LogLevel.Critical, this);
                Logger.Log($"--> OUT MSG: {frame.Replace("\r", "(CR)")}", LogLevel.Critical, this);

                //read
                List<byte> totalResponse = new List<byte>();
                byte[] responseBuffer = new byte[512];

                bool wasMultiFramedResponse = false;
                CommandState cmdState = CommandState.Intial;

                //read until command complete
                while (cmdState != CommandState.Complete) {

                    //time measuring
                    if (speedStopwatchDownstr == null) {
                        speedStopwatchDownstr = Stopwatch.StartNew();
                    }

                    if (speedStopwatchDownstr.Elapsed.TotalSeconds >= 1) {
                        speedStopwatchDownstr.Restart();
                        bytesTotalCountedDownstream = 0;
                    }

                    responseBuffer = new byte[128];

                    await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                    bool terminatorReceived = responseBuffer.Any(x => x == (byte)CR);
                    var delimiterTerminatorIdx = SearchBytePattern(responseBuffer, new byte[] { (byte)DELIMITER, (byte)CR });

                    if (terminatorReceived && delimiterTerminatorIdx == -1) {
                        cmdState = CommandState.Complete;
                    } else if (delimiterTerminatorIdx != -1) {
                        cmdState = CommandState.RequestedNextFrame;
                    } else {
                        cmdState = CommandState.LineFeed;
                    }

                    //log message parts
                    var tempMsg = Encoding.UTF8.GetString(responseBuffer).Replace("\r", "(CR)");
                    Logger.Log($">> IN PART: {tempMsg}, Command state: {cmdState}", LogLevel.Critical, this);

                    //error response
                    int errorCode = CheckForErrorMsg(tempMsg);
                    if (errorCode != 0) return new MewtocolFrameResponse(errorCode);

                    //add complete response to collector without empty bytes
                    totalResponse.AddRange(responseBuffer.Where(x => x != (byte)0x0));

                    //request next part of the command if the delimiter was received
                    if (cmdState == CommandState.RequestedNextFrame) {

                        Logger.Log($"Requesting next frame...", LogLevel.Critical, this);

                        wasMultiFramedResponse = true;
                        writeBuffer = Encoding.UTF8.GetBytes("%01**&\r");
                        await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length);

                    }

                }

                //build final result
                string resString = Encoding.UTF8.GetString(totalResponse.ToArray());

                if (wasMultiFramedResponse) {

                    var split = resString.Split('&');

                    for (int j = 0; j < split.Length; j++) {

                        split[j] = split[j].Replace("\r", "");
                        split[j] = split[j].Substring(0, split[j].Length - 2);
                        if (j > 0) split[j] = split[j].Replace($"%{GetStationNumber()}", "");

                    }

                    resString = string.Join("", split);

                }

                bytesTotalCountedDownstream += Encoding.ASCII.GetByteCount(resString);

                var perSecDownstream = (double)((bytesTotalCountedDownstream / speedStopwatchDownstr.Elapsed.TotalMilliseconds) * 1000);

                if (perSecUpstream <= 10000)
                    BytesPerSecondDownstream = (int)Math.Round(perSecUpstream, MidpointRounding.AwayFromZero);

                Logger.Log($"<-- IN MSG: {resString.Replace("\r", "(CR)")}", LogLevel.Critical, this);
                Logger.Log($"Total bytes parsed: {resString.Length}", LogLevel.Critical, this);
                Logger.Log($"[---------CMD END----------]", LogLevel.Critical, this);

                return new MewtocolFrameResponse(resString);

            } catch (Exception ex) {

                return new MewtocolFrameResponse(400, ex.Message.ToString(System.Globalization.CultureInfo.InvariantCulture));

            }

        }

        private int CheckForErrorMsg (string msg) {

            //error catching
            Regex errorcheck = new Regex(@"\%[0-9]{2}\!([0-9]{2})", RegexOptions.IgnoreCase);
            Match m = errorcheck.Match(msg);

            if (m.Success) {

                string eCode = m.Groups[1].Value;
                return Convert.ToInt32(eCode);

            }

            return 0;

        }

        private int SearchBytePattern (byte[] src, byte[] pattern) {

            int maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++) {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--) {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        
        }

        #endregion

        #region Disposing

        private void OnMajorSocketExceptionWhileConnecting() {

            if (IsConnected) {

                Logger.Log("The PLC connection timed out", LogLevel.Error, this);
                OnDisconnect();

            }

        }

        private void OnMajorSocketExceptionWhileConnected() {

            if (IsConnected) {

                Logger.Log("The PLC connection was closed", LogLevel.Error, this);
                OnDisconnect();

            }

        }


        /// <summary>
        /// Disposes the current interface and clears all its members
        /// </summary>
        public void Dispose() {

            if (Disposed) return;

            Disconnect();

            //GC.SuppressFinalize(this);

            Disposed = true;

        }

        private void OnDisconnect () {

            if (IsConnected) {

                BytesPerSecondDownstream = 0;
                BytesPerSecondUpstream = 0;
                CycleTimeMs = 0;

                IsConnected = false;
                ClearRegisterVals();

                Disconnected?.Invoke();
                KillPoller();
                client.Close();

            }

        }


        private void ClearRegisterVals() {

            for (int i = 0; i < RegistersUnderlying.Count; i++) {

                var reg = (IRegisterInternal)RegistersUnderlying[i];
                reg.ClearValue();

            }

        }

        #endregion

        #region Accessing Info 

        /// <summary>
        /// Gets the connection info string
        /// </summary>
        public string GetConnectionPortInfo() {

            return $"{IpAddress}:{Port}";

        }

        #endregion

        #region Property change evnts

        /// <summary>
        /// Triggers a property changed event
        /// </summary>
        /// <param name="propertyName">Name of the property to trigger for</param>
        private void OnPropChange ([CallerMemberName]string propertyName = null) {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        #endregion

    }

}