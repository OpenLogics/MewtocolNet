using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MewtocolNet.Registers;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Logging;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Net;
using System.Threading;
using MewtocolNet.Queue;
using System.Reflection;
using System.Timers;
using System.Data;
using System.Xml.Linq;

namespace MewtocolNet
{

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

        private volatile int pollerDelayMs = 0;
        /// <summary>
        /// Delay for each poller cycle in milliseconds, default = 0
        /// </summary>
        public int PollerDelayMs {
            get => pollerDelayMs;
            set { 
                pollerDelayMs = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PollerDelayMs)));
            }
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
        public List<IRegister> Registers { get; set; } = new List<IRegister>();

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

        #region Initialization

        /// <summary>
        /// Builds a new Interfacer for a PLC
        /// </summary>
        /// <param name="_ip">IP adress of the PLC</param>
        /// <param name="_port">Port of the PLC</param>
        /// <param name="_station">Station Number of the PLC</param>
        public MewtocolInterface (string _ip, int _port = 9094, int _station = 1) {

            ip = _ip;
            port = _port;
            stationNumber = _station;

            Connected += MewtocolInterface_Connected;

            void MewtocolInterface_Connected (PLCInfo obj) {

                if (usePoller)
                    AttachPoller();

                IsConnected = true;

            }

            RegisterChanged += (o) => {

                string address = $"{o.GetRegisterString()}{o.GetStartingMemoryArea()}".PadRight(5, (char)32);

                Logger.Log($"{address} " +
                           $"{(o.Name != null ? $"({o.Name}) " : "")}" +
                           $"changed to \"{o.GetValueString()}\"", LogLevel.Change, this);
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
        public async Task<MewtocolInterface> ConnectAsync (Action<PLCInfo> OnConnected = null, Action OnFailed = null) {

            Logger.Log("Connecting to PLC...", LogLevel.Info, this);

            var plcinf = await GetPLCInfoAsync();

            if (plcinf != null) {

                Logger.Log("Connected", LogLevel.Info, this);
                Logger.Log($"\n\n{plcinf.ToString()}\n\n", LogLevel.Verbose, this);

                Connected?.Invoke(plcinf);

                if (OnConnected != null) {

                    if (!usePoller) {
                        OnConnected(plcinf);
                        return this;
                    }

                    PolledCycle += OnPollCycleDone;
                    void OnPollCycleDone () {
                        OnConnected(plcinf);
                        PolledCycle -= OnPollCycleDone;
                    }
                }

            } else {

                if (OnFailed != null) {
                    OnFailed();
                    Disconnected?.Invoke();
                    Logger.Log("Initial connection failed", LogLevel.Info, this);
                }

            }

            return this;

        }

        /// <summary>
        /// Changes the connections parameters of the PLC, only applyable when the connection is offline
        /// </summary>
        /// <param name="_ip">Ip adress</param>
        /// <param name="_port">Port number</param>
        /// <param name="_station">Station number</param>
        public void ChangeConnectionSettings (string _ip, int _port, int _station = 1) {

            if (IsConnected)
                throw new Exception("Cannot change the connection settings while the PLC is connected");

            ip = _ip;
            port = _port;
            stationNumber = _station;

        }

        /// <summary>
        /// Closes the connection all cyclic polling 
        /// </summary>
        public void Disconnect () {

            if (!IsConnected)
                return;

            OnMajorSocketExceptionWhileConnected();

        }

        /// <summary>
        /// Attaches a poller to the interface that continously 
        /// polls the registered data registers and writes the values to them
        /// </summary>
        public MewtocolInterface WithPoller () {

            usePoller = true;

            return this;

        }

        #endregion

        #region TCP connection state handling

        private async Task ConnectTCP () {

            if (!IPAddress.TryParse(ip, out var targetIP)) {
                throw new ArgumentException("The IP adress of the PLC was no valid format");
            }

            try {

                if(HostEndpoint != null) {
                    
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

                if(!success || !client.Connected) {
                    OnMajorSocketExceptionWhileConnecting();
                    return;
                }

                if(HostEndpoint == null) {
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

        private void OnMajorSocketExceptionWhileConnecting () {

            Logger.Log("The PLC connection timed out", LogLevel.Error, this);
            CycleTimeMs = 0;
            IsConnected = false;
            KillPoller();

        }

        private void OnMajorSocketExceptionWhileConnected () {

            if (IsConnected) {

                Logger.Log("The PLC connection was closed", LogLevel.Error, this);
                CycleTimeMs = 0;
                IsConnected = false;
                Disconnected?.Invoke();
                KillPoller();
                client.Close();

            }

        }

        private void ClearRegisterVals () {

            for (int i = 0; i < Registers.Count; i++) {

                var reg = Registers[i];
                reg.ClearValue();

            }

        }

        #endregion

        #region Register Collection

        /// <summary>
        /// Attaches a register collection object to 
        /// the interface that can be updated automatically.
        /// <para/>
        /// Just create a class inheriting from <see cref="RegisterCollectionBase"/>
        /// and assert some propertys with the custom <see cref="RegisterAttribute"/>.
        /// </summary>
        /// <param name="collection">A collection inherting the <see cref="RegisterCollectionBase"/> class</param>
        public MewtocolInterface WithRegisterCollection (RegisterCollectionBase collection) {

            collection.PLCInterface = this;

            var props = collection.GetType().GetProperties();

            foreach (var prop in props) {

                var attributes = prop.GetCustomAttributes(true);

                string propName = prop.Name;
                foreach (var attr in attributes) {

                    if (attr is RegisterAttribute cAttribute) {

                        if (prop.PropertyType == typeof(bool) && cAttribute.AssignedBitIndex == -1) {

                            //add bool register non bit assgined
                            Registers.Add(new BRegister((IOType)(int)cAttribute.RegisterType, cAttribute.SpecialAddress, cAttribute.MemoryArea, _name: propName).WithCollectionType(collection.GetType()));

                        }

                        if (prop.PropertyType == typeof(short)) {
                            AddRegister<short>(collection.GetType(), cAttribute.MemoryArea, prop);
                        }

                        if (prop.PropertyType == typeof(ushort)) {
                            AddRegister<ushort>(collection.GetType(), cAttribute.MemoryArea, prop);
                        }

                        if (prop.PropertyType == typeof(int)) {
                            AddRegister<int>(collection.GetType(), cAttribute.MemoryArea, prop);
                        }

                        if (prop.PropertyType == typeof(uint)) {
                            AddRegister<uint>(collection.GetType(), cAttribute.MemoryArea, prop);
                        }

                        if (prop.PropertyType == typeof(float)) {
                            AddRegister<float>(collection.GetType(), cAttribute.MemoryArea, prop);
                        }

                        if (prop.PropertyType == typeof(string)) {
                            AddRegister<string>(collection.GetType(), cAttribute.MemoryArea, prop, cAttribute.StringLength);
                        }

                        if (prop.PropertyType.IsEnum) {

                            if (cAttribute.BitCount == BitCount.B16) {
                                AddRegister<short>(collection.GetType(), cAttribute.MemoryArea, prop, _enumType: prop.PropertyType);
                            } else {
                                AddRegister<int>(collection.GetType(), cAttribute.MemoryArea, prop, _enumType: prop.PropertyType);
                            }

                        }

                        //read number as bit array
                        if (prop.PropertyType == typeof(BitArray)) {

                            if (cAttribute.BitCount == BitCount.B16) {
                                AddRegister<short>(collection.GetType(), cAttribute.MemoryArea, prop, _isBitwise: true);
                            } else {
                                AddRegister<int>(collection.GetType(), cAttribute.MemoryArea, prop, _isBitwise: true);
                            }

                        }

                        //read number as bit array by invdividual properties
                        if (prop.PropertyType == typeof(bool) && cAttribute.AssignedBitIndex != -1) {

                            //var bitwiseCount = Registers.Count(x => x.Value.isUsedBitwise);

                            if (cAttribute.BitCount == BitCount.B16) {
                                AddRegister<short>(collection.GetType(), cAttribute.MemoryArea, prop, _isBitwise: true);
                            } else {
                                AddRegister<int>(collection.GetType(), cAttribute.MemoryArea, prop, _isBitwise: true);
                            }

                        }

                        if (prop.PropertyType == typeof(TimeSpan)) {
                            AddRegister<TimeSpan>(collection.GetType(), cAttribute.MemoryArea, prop);
                        }

                    }

                }

            }

            RegisterChanged += (reg) => {

                //register is used bitwise
                if(reg.IsUsedBitwise()) {

                    for (int i = 0; i < props.Length; i++) {

                        var prop = props[i];
                        var bitWiseFound = prop.GetCustomAttributes(true)
                        .FirstOrDefault(y => y.GetType() == typeof(RegisterAttribute) && ((RegisterAttribute)y).MemoryArea == reg.MemoryAddress);

                        if(bitWiseFound != null) {

                            var casted = (RegisterAttribute)bitWiseFound;
                            var bitIndex = casted.AssignedBitIndex;

                            BitArray bitAr = null;

                            if (reg is NRegister<short> reg16) {
                                var bytes = BitConverter.GetBytes((short)reg16.Value);
                                bitAr = new BitArray(bytes);
                            } else if(reg is NRegister<int> reg32) {
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

        #region Register Writing

        /// <summary>
        /// Sets a register in the PLCs memory
        /// </summary>
        /// <param name="registerName">The name the register was given to or a property name from the RegisterCollection class</param>
        /// <param name="value">The value to write to the register</param>
        public void SetRegister (string registerName, object value) {

            var foundRegister = GetAllRegisters().FirstOrDefault(x => x.Name == registerName);

            if (foundRegister == null) {
                throw new Exception($"Register with the name {registerName} was not found");
            }

            _ = SetRegisterAsync(registerName, value);

        }

        /// <summary>
        /// Sets a register in the PLCs memory asynchronously, returns the result status from the PLC
        /// </summary>
        /// <param name="registerName">The name the register was given to or a property name from the RegisterCollection class</param>
        /// <param name="value">The value to write to the register</param>
        public async Task<bool> SetRegisterAsync (string registerName, object value) {

            var foundRegister = GetAllRegisters().FirstOrDefault(x => x.Name == registerName);

            if (foundRegister == null) {
                throw new Exception($"Register with the name {registerName} was not found");
            }

            if (foundRegister.GetType() == typeof(BRegister)) {

                return await WriteBoolRegister((BRegister)foundRegister, (bool)value);

            }

            if (foundRegister.GetType() == typeof(NRegister<short>)) {

                return await WriteNumRegister((NRegister<short>)foundRegister, (short)value);

            }

            if (foundRegister.GetType() == typeof(NRegister<ushort>)) {

                return await WriteNumRegister((NRegister<ushort>)foundRegister, (ushort)value);

            }

            if (foundRegister.GetType() == typeof(NRegister<int>)) {

                return await WriteNumRegister((NRegister<int>)foundRegister, (int)value);

            }

            if (foundRegister.GetType() == typeof(NRegister<uint>)) {

                return await WriteNumRegister((NRegister<uint>)foundRegister, (uint)value);

            }

            if (foundRegister.GetType() == typeof(NRegister<float>)) {

                return await WriteNumRegister((NRegister<float>)foundRegister, (float)value);

            }

            if (foundRegister.GetType() == typeof(NRegister<TimeSpan>)) {

                return await WriteNumRegister((NRegister<TimeSpan>)foundRegister, (TimeSpan)value);

            }

            if (foundRegister.GetType() == typeof(SRegister)) {

                return await WriteStringRegister((SRegister)foundRegister, (string)value);

            }

            return false;

        }

        #endregion

        #region Low level command handling

        /// <summary>
        /// Calculates checksum and sends a command to the PLC then awaits results
        /// </summary>
        /// <param name="_msg">MEWTOCOL Formatted request string ex: %01#RT</param>
        /// <returns>Returns the result</returns>
        public async Task<CommandResult> SendCommandAsync (string _msg) {

            _msg = _msg.BuildBCCFrame();
            _msg += "\r";

            //send request
            try {

                queuedMessages++;
                
                var response = await queue.Enqueue(() => SendSingleBlock(_msg));

                if (queuedMessages > 0) 
                    queuedMessages--;

                if (response == null) {
                    return new CommandResult {
                        Success = false,
                        Error = "0000",
                        ErrorDescription = "null result"
                    };
                }

                //error catching
                Regex errorcheck = new Regex(@"\%[0-9]{2}\!([0-9]{2})", RegexOptions.IgnoreCase);
                Match m = errorcheck.Match(response.ToString());
                if (m.Success) {
                    string eCode = m.Groups[1].Value;
                    string eDes = Links.CodeDescriptions.Error[Convert.ToInt32(eCode)];
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Response is: {response}");
                    Logger.Log($"Error on command {_msg.Replace("\r", "")} the PLC returned error code: {eCode}, {eDes}", LogLevel.Error);
                    Console.ResetColor();
                    return new CommandResult {
                        Success = false,
                        Error = eCode,
                        ErrorDescription = eDes
                    };
                }

                return new CommandResult {
                    Success = true,
                    Error = "0000",
                    Response = response.ToString()
                };

            } catch {
                return new CommandResult {
                    Success = false,
                    Error = "0000",
                    ErrorDescription = "null result"
                };
            }

        }

        private async Task<string> SendSingleBlock (string _blockString) {

            if (client == null || !client.Connected ) {
                await ConnectTCP();
            }

            if (client == null || !client.Connected)
                return null;

            var message = _blockString.ToHexASCIIBytes();

            //time measuring
            if(speedStopwatchUpstr == null) {
                speedStopwatchUpstr = Stopwatch.StartNew();
            }

            if(speedStopwatchUpstr.Elapsed.TotalSeconds >= 1) {
                speedStopwatchUpstr.Restart();
                bytesTotalCountedUpstream = 0;
            }

            //send request
            using (var sendStream = new MemoryStream(message)) {
                await sendStream.CopyToAsync(stream);
                Logger.Log($"[--------------------------------]", LogLevel.Critical, this);
                Logger.Log($"--> OUT MSG: {_blockString}", LogLevel.Critical, this);
            }

            //calc upstream speed
            bytesTotalCountedUpstream += message.Length;

            var perSecUpstream = (double)((bytesTotalCountedUpstream / speedStopwatchUpstr.Elapsed.TotalMilliseconds) * 1000);
            if (perSecUpstream <= 10000)
                BytesPerSecondUpstream = (int)Math.Round(perSecUpstream, MidpointRounding.AwayFromZero);

            //await result
            StringBuilder response = new StringBuilder();
            try {

                byte[] responseBuffer = new byte[128 * 16];

                bool endLineCode = false;
                bool startMsgCode = false;

                while (!endLineCode && !startMsgCode) {

                    do {

                        //time measuring
                        if (speedStopwatchDownstr == null) {
                            speedStopwatchDownstr = Stopwatch.StartNew();
                        }

                        if (speedStopwatchDownstr.Elapsed.TotalSeconds >= 1) {
                            speedStopwatchDownstr.Restart();
                            bytesTotalCountedDownstream = 0;
                        }

                        int bytes = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                        endLineCode = responseBuffer.Any(x => x == 0x0D);
                        startMsgCode = responseBuffer.Count(x => x == 0x25) > 1;

                        if (!endLineCode && !startMsgCode) break;

                        response.Append(Encoding.UTF8.GetString(responseBuffer, 0, bytes));
                    }
                    while (stream.DataAvailable);

                }

            } catch (IOException) {
                OnMajorSocketExceptionWhileConnected();
                return null;
            } catch (SocketException) {
                OnMajorSocketExceptionWhileConnected();
                return null;
            }

            if(!string.IsNullOrEmpty(response.ToString())) {
                
                Logger.Log($"<-- IN MSG: {response}", LogLevel.Critical, this);

                bytesTotalCountedDownstream += Encoding.ASCII.GetByteCount(response.ToString());

                var perSecDownstream = (double)((bytesTotalCountedDownstream / speedStopwatchDownstr.Elapsed.TotalMilliseconds) * 1000);

                if(perSecUpstream <= 10000) 
                    BytesPerSecondDownstream = (int)Math.Round(perSecUpstream, MidpointRounding.AwayFromZero);

                return response.ToString();

            } else {
                return null;
            }

        }

        #endregion

        #region Disposing

        /// <summary>
        /// Disposes the current interface and clears all its members
        /// </summary>
        public void Dispose () {

            if (Disposed) return;

            Disconnect();

            GC.SuppressFinalize(this);

            Disposed = true;

        }

        #endregion

        #region Accessing Info 

        /// <summary>
        /// Gets the connection info string
        /// </summary>
        public string GetConnectionPortInfo () {

            return $"{IpAddress}:{Port}";

        }

        #endregion

    }


}