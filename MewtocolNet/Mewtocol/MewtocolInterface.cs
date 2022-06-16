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
    
    public partial class MewtocolInterface {

        /// <summary>
        /// Generic information about the connected PLC
        /// </summary>
        public PLCInfo PlcInfo {get;private set;}
        private CancellationTokenSource tokenSource;

        private string ip {get;set;}
        private int port {get;set;}
        public int ConnectionTimeout {get;set;} = 2000;

        #region Initialization
        /// <summary>
        /// Builds a new Interfacer for a PLC
        /// </summary>
        /// <param name="_ip"></param>
        /// <param name="_port"></param>
        public MewtocolInterface (string _ip, int _port = 9094) {
            ip = _ip;
            port = _port;
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