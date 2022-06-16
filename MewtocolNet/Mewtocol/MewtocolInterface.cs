using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MewtocolNet.Responses;

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
        public Dictionary<int, Register> Registers { get; set; } = new();

        private CancellationTokenSource tokenSource;

        private string ip {get;set;}
        private int port {get;set;}
        private int stationNumber {get;set;}        
        private int pollingDelayMs {get;set;}

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

            if (plcinf is not null) {

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

                if (OnFailed != null)
                    OnFailed();

            }

            return this;

        }

        /// <summary>
        /// Attaches a poller to the interface that continously 
        /// polls the registered data registers and writes the values to them
        /// </summary>
        public MewtocolInterface WithPoller (int pollerDelayMs = 50) {

            pollingDelayMs = pollerDelayMs;
            usePoller = true;

            return this;

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
            var response = await SendSingleBlock(_msg);

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

            if(isWriting) {
                return null;
            }

            tokenSource = new CancellationTokenSource();

            using (TcpClient client = new TcpClient() { ReceiveBufferSize = 64, NoDelay = true, ExclusiveAddressUse = true }) {

                try {
                    await client.ConnectAsync(ip, port, tokenSource.Token);
                } catch(SocketException) {
                    return null;
                }
                
                using (NetworkStream stream = client.GetStream()) {
                    var message = _blockString.ToHexASCIIBytes();
                    var messageAscii = BitConverter.ToString(message).Replace("-", " ");
                    //send request
                    isWriting = true;
                    using (var sendStream = new MemoryStream(message)) {
                        await sendStream.CopyToAsync(stream);
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
                    isWriting = false;
                    return response.ToString();
                }

            }

        }


        #endregion
    }

}