using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MewtocolNet.Responses;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Logging;
using System.Collections;
using System.Reflection;
using MewtocolNet.Logging;
using System.Diagnostics;

namespace MewtocolNet {
    
    /// <summary>
    /// The PLC com interface class
    /// </summary>
    public partial class MewtocolInterface {

        /// <summary>
        /// Gets triggered when the PLC connection was established
        /// </summary>
        public event Action<PLCInfo> Connected;

        /// <summary>
        /// Gets triggered when a registered data register changes its value
        /// </summary>
        public event Action<Register> RegisterChanged;

        /// <summary>
        /// Generic information about the connected PLC
        /// </summary>
        public PLCInfo PlcInfo {get;private set;}

        /// <summary>
        /// The registered data registers of the PLC
        /// </summary>
        public Dictionary<int, Register> Registers { get; set; } = new Dictionary<int, Register>();

        private string ip {get;set;}
        private int port {get;set;}
        private int stationNumber {get;set;}        

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

        internal List<Task> PriorityTasks { get; set; } = new List<Task>(); 

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

            }

            RegisterChanged += (o) => {

                string address = $"{o.GetRegisterString()}{o.MemoryAdress}".PadRight(5, (char)32); ;

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
                    Logger.Log("Initial connection failed", LogLevel.Info, this);
                }

            }

            return this;

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

                    if(attr is RegisterAttribute cAttribute) {

                        if (prop.PropertyType == typeof(bool) && cAttribute.AssignedBitIndex == -1) {
                            if (cAttribute.SpecialAddress == SpecialAddress.None) {
                                AddRegister(cAttribute.MemoryArea, cAttribute.RegisterType, _name: propName);
                            } else {
                                AddRegister(cAttribute.SpecialAddress, cAttribute.RegisterType, _name: propName);
                            }
                        }

                        if (prop.PropertyType == typeof(short)) {
                            AddRegister<short>(cAttribute.MemoryArea, _name: propName);
                        }

                        if (prop.PropertyType == typeof(ushort)) {
                            AddRegister<ushort>(cAttribute.MemoryArea, _name: propName);
                        }

                        if (prop.PropertyType == typeof(int)) {
                            AddRegister<int>(cAttribute.MemoryArea, _name: propName);
                        }

                        if (prop.PropertyType == typeof(uint)) {
                            AddRegister<uint>(cAttribute.MemoryArea, _name: propName);
                        }

                        if (prop.PropertyType == typeof(float)) {
                            AddRegister<float>(cAttribute.MemoryArea, _name: propName);
                        }

                        if (prop.PropertyType == typeof(string)) {
                            AddRegister<string>(cAttribute.MemoryArea, cAttribute.StringLength, _name: propName);
                        }

                        //read number as bit array
                        if (prop.PropertyType == typeof(BitArray)) {

                            if(cAttribute.BitCount == BitCount.B16) {
                                AddRegister<short>(cAttribute.MemoryArea, _name: propName, _isBitwise: true);
                            } else {
                                AddRegister<int>(cAttribute.MemoryArea, _name: propName, _isBitwise: true);
                            }

                        }

                        //read number as bit array by invdividual properties
                        if (prop.PropertyType == typeof(bool) && cAttribute.AssignedBitIndex != -1) {

                            if (cAttribute.BitCount == BitCount.B16) {
                                AddRegister<short>(cAttribute.MemoryArea, _name: propName, _isBitwise: true);
                            } else {
                                AddRegister<int>(cAttribute.MemoryArea, _name: propName, _isBitwise: true);
                            }

                        }

                    }

                }

            }

            RegisterChanged += (reg) => {

                var foundToUpdate = props.FirstOrDefault(x => x.Name == reg.Name);
               
                if (foundToUpdate != null) {

                    var foundAttributes = foundToUpdate.GetCustomAttributes(true);
                    var foundAttr = foundAttributes.FirstOrDefault(x => x.GetType() == typeof(RegisterAttribute));

                    if (foundAttr == null)
                        return;

                    var registerAttr = (RegisterAttribute)foundAttr;

                    //check if bit parse mode
                    if (registerAttr.AssignedBitIndex == -1) {

                        //setting back booleans
                        if (foundToUpdate.PropertyType == typeof(bool)) {
                            foundToUpdate.SetValue(collection, ((BRegister)reg).Value);
                        }

                        //setting back numbers

                        if (foundToUpdate.PropertyType == typeof(short)) {
                            foundToUpdate.SetValue(collection, ((NRegister<short>)reg).Value);
                        }

                        if (foundToUpdate.PropertyType == typeof(ushort)) {
                            foundToUpdate.SetValue(collection, ((NRegister<ushort>)reg).Value);
                        }

                        if (foundToUpdate.PropertyType == typeof(int)) {
                            foundToUpdate.SetValue(collection, ((NRegister<int>)reg).Value);
                        }

                        if (foundToUpdate.PropertyType == typeof(uint)) {
                            foundToUpdate.SetValue(collection, ((NRegister<uint>)reg).Value);
                        }

                        if (foundToUpdate.PropertyType == typeof(float)) {
                            foundToUpdate.SetValue(collection, ((NRegister<float>)reg).Value);
                        }

                        //setting back strings

                        if (foundToUpdate.PropertyType == typeof(string)) {
                            foundToUpdate.SetValue(collection, ((SRegister)reg).Value);
                        }

                    }


                    if (foundToUpdate.PropertyType == typeof(bool) && registerAttr.AssignedBitIndex >= 0) {

                        //setting back bit registers to individual properties
                        if (reg is NRegister<short> shortReg) {

                            var bytes = BitConverter.GetBytes(shortReg.Value);
                            BitArray bitAr = new BitArray(bytes);
                            foundToUpdate.SetValue(collection, bitAr[registerAttr.AssignedBitIndex]);

                        }

                        if (reg is NRegister<int> intReg) {

                            var bytes = BitConverter.GetBytes(intReg.Value);
                            BitArray bitAr = new BitArray(bytes);
                            foundToUpdate.SetValue(collection, bitAr[registerAttr.AssignedBitIndex]);

                        }


                    } else if(foundToUpdate.PropertyType == typeof(BitArray)) {

                        //setting back bit registers
                        if (reg is NRegister<short> shortReg) {

                            var bytes = BitConverter.GetBytes(shortReg.Value);
                            BitArray bitAr = new BitArray(bytes);
                            foundToUpdate.SetValue(collection, bitAr);

                        }

                        if (reg is NRegister<int> intReg) {

                            var bytes = BitConverter.GetBytes(intReg.Value);
                            BitArray bitAr = new BitArray(bytes);
                            foundToUpdate.SetValue(collection, bitAr);

                        }

                    }

                    collection.TriggerPropertyChanged(foundToUpdate.Name);

                }

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

            if (foundRegister.GetType() == typeof(BRegister)) {

                _ = WriteBoolRegister((BRegister)foundRegister, (bool)value, StationNumber);

            }

            if (foundRegister.GetType() == typeof(NRegister<short>)) {

                _ = WriteNumRegister((NRegister<short>)foundRegister, (short)value, StationNumber);
                
            }

            if (foundRegister.GetType() == typeof(NRegister<ushort>)) {

                _ = WriteNumRegister((NRegister<ushort>)foundRegister, (ushort)value, StationNumber);

            }

            if (foundRegister.GetType() == typeof(NRegister<int>)) {

                _ = WriteNumRegister((NRegister<int>)foundRegister, (int)value, StationNumber);

            }

            if (foundRegister.GetType() == typeof(NRegister<uint>)) {

                _ = WriteNumRegister((NRegister<uint>)foundRegister, (uint)value, StationNumber);

            }

            if (foundRegister.GetType() == typeof(NRegister<float>)) {

                _ = WriteNumRegister((NRegister<float>)foundRegister, (float)value, StationNumber);

            }

            if (foundRegister.GetType() == typeof(SRegister)) {

                _ = WriteStringRegister((SRegister)foundRegister, (string)value, StationNumber);

            }

        }

        #endregion

        #region Low level command handling

        /// <summary>
        /// Sends a command to the PLC and awaits results
        /// </summary>
        /// <param name="_msg">MEWTOCOL Formatted request string ex: %01#RT</param>
        /// <param name="_close">Auto close of frame [true]%01#RT01\r [false]%01#RT</param>
        /// <returns>Returns the result</returns>
        public async Task<CommandResult> SendCommandAsync (string _msg) {

            _msg = _msg.BuildBCCFrame();
            _msg += "\r";

            //send request

            string response = null;

            if(ContinousReaderRunning) {

                //if the poller is active then add all messages to a qeueue

                var awaittask = SendSingleBlock(_msg);
                PriorityTasks.Add(awaittask);
                awaittask.Wait();

                PriorityTasks.Remove(awaittask);    
                response = awaittask.Result;

            } else {

                //poller not active let the user manage message timing

                response = await SendSingleBlock(_msg);

            }

            if(response == null) {
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
                string eDes = Links.LinkedData.ErrorCodes[Convert.ToInt32(eCode)];
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Response is: {response}");
                Console.WriteLine($"Error on command {_msg.Replace("\r", "")} the PLC returned error code: {eCode}, {eDes}");
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

        }

        private async Task<string> SendSingleBlock (string _blockString) {

            Stopwatch sw = Stopwatch.StartNew();    

            using (TcpClient client = new TcpClient() { ReceiveBufferSize = 64, NoDelay = true, ExclusiveAddressUse = true }) {

                try {
                    await client.ConnectAsync(ip, port);
                } catch(SocketException) {
                    return null;
                }
                
                using (NetworkStream stream = client.GetStream()) {
                    var message = _blockString.ToHexASCIIBytes();
                    var messageAscii = BitConverter.ToString(message).Replace("-", " ");
                    //send request
                    using (var sendStream = new MemoryStream(message)) {
                        await sendStream.CopyToAsync(stream);
                        Logger.Log($"OUT MSG: {_blockString}", LogLevel.Critical, this);
                        //log message sent
                        ASCIIEncoding enc = new ASCIIEncoding();
                        string characters = enc.GetString(message);
                    }
                    //await result
                    StringBuilder response = new StringBuilder();
                    byte[] responseBuffer = new byte[256];
                    do {
                        int bytes = stream.Read(responseBuffer, 0, responseBuffer.Length);
                        response.Append(Encoding.UTF8.GetString(responseBuffer, 0, bytes));
                    }
                    while (stream.DataAvailable);
                    sw.Stop();
                    Logger.Log($"IN MSG ({(int)sw.Elapsed.TotalMilliseconds}ms): {_blockString}", LogLevel.Critical, this);
                    return response.ToString();
                }

            }

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