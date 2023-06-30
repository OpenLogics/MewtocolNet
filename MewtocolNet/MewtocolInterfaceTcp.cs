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
using System.IO.Ports;
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
    public class MewtocolInterfaceTcp : MewtocolInterface, IPlcEthernet {

        /// <summary>
        /// The host ip endpoint, leave it null to use an automatic interface
        /// </summary>
        public IPEndPoint HostEndpoint { get; set; }

        //TCP
        internal TcpClient client;

        //tcp/ip config
        private string ip;
        private int port;

        /// <inheritdoc/>
        public string IpAddress => ip;

        /// <inheritdoc/>
        public int Port => port;

        internal MewtocolInterfaceTcp () : base() { }

        /// <inheritdoc/>
        public IPlcEthernet WithPoller () {

            usePoller = true;   
            return this;
       
        }

        #region TCP connection state handling

        /// <inheritdoc/>
        public void ConfigureConnection (string _ip, int _port = 9094, int _station = 1) {

            ip = _ip;
            port = _port;
            stationNumber = _station;

            Disconnect();

        }

        /// <inheritdoc/>
        public override async Task ConnectAsync () {

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

                //get the stream
                stream = client.GetStream();
                stream.ReadTimeout = 1000;

                //get plc info
                var plcinf = await GetPLCInfoAsync();

                if (plcinf != null) {

                    OnConnected(plcinf);

                } else {

                    Logger.Log("Initial connection failed", LogLevel.Info, this);
                    OnDisconnect();

                }

                await Task.CompletedTask;

            } catch (SocketException) {

                OnMajorSocketExceptionWhileConnecting();

            }

        }

        /// <summary>
        /// Gets the connection info string
        /// </summary>
        public override string GetConnectionInfo() {

            return $"{IpAddress}:{Port}";

        }

        private protected override void OnDisconnect() {

            if (IsConnected) {

                base.OnDisconnect();

                client.Close();

            }

        }

        #endregion

    }

}