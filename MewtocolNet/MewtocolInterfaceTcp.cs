using MewtocolNet.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MewtocolNet {

    /// <summary>
    /// The PLC com interface class
    /// </summary>
    public sealed class MewtocolInterfaceTcp : MewtocolInterface, IPlcEthernet {

        //TCP
        internal TcpClient client;

        private IPAddress ipAddr;

        /// <inheritdoc/>
        public string IpAddress => ipAddr.ToString();

        /// <inheritdoc/>
        public int Port { get; private set; }

        /// <inheritdoc/>
        public IPEndPoint HostEndpoint { get; set; }

        internal MewtocolInterfaceTcp() : base() { }

        #region TCP connection state handling

        /// <inheritdoc/>
        public void ConfigureConnection(string ip, int port = 9094, int station = 0xEE) {

            if (!IPAddress.TryParse(ip, out ipAddr))
                throw new NotSupportedException($"The ip: {ip} is no valid ip address");

            if (stationNumber != 0xEE && stationNumber > 99)
                throw new NotSupportedException("Station number can't be greater than 99");

            Port = port;
            stationNumber = station;

            Disconnect();

        }

        /// <inheritdoc/>
        public void ConfigureConnection(IPAddress ip, int port = 9094, int station = 0xEE) {

            ipAddr = ip;
            Port = port;

            if (stationNumber != 0xEE && stationNumber > 99)
                throw new NotSupportedException("Station number can't be greater than 99");

            stationNumber = station;

            Disconnect();

        }

        /// <inheritdoc/>
        public override async Task ConnectAsync(Func<Task> callBack = null) {

            try {

                Logger.Log($">> Intial connection start <<", LogLevel.Verbose, this);
                isConnectingStage = true;

                if (HostEndpoint != null) {

                    client = new TcpClient(HostEndpoint) {
                        ReceiveBufferSize = RecBufferSize,
                        NoDelay = false,
                    };
                    var ep = (IPEndPoint)client.Client.LocalEndPoint;
                    Logger.Log($"Connecting [MAN] endpoint: {ep.Address}:{ep.Port}", LogLevel.Info, this);

                } else {

                    client = new TcpClient() {
                        ReceiveBufferSize = RecBufferSize,
                        NoDelay = false,
                        //ExclusiveAddressUse = true,
                    };

                }

                var result = client.BeginConnect(ipAddr, Port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(ConnectTimeout));

                if (!success || !client.Connected) {

                    Logger.Log("The PLC connection timed out", LogLevel.Error, this);
                    OnMajorSocketExceptionWhileConnecting();
                    return;
                }

                if (HostEndpoint == null) {
                    var ep = (IPEndPoint)client.Client.LocalEndPoint;
                    Logger.Log($"Connecting [AUTO] endpoint: {ep.Address.MapToIPv4()}:{ep.Port}", LogLevel.Info, this);
                }

                //get the stream
                stream = client.GetStream();
                stream.ReadTimeout = 1000;

                //get plc info
                var plcinf = await GetPLCInfoAsync();

                if (plcinf != null) {

                    IsConnected = true;
                    await base.ConnectAsync(callBack);
                    OnConnected(plcinf);

                } else {

                    Logger.Log("Initial connection failed", LogLevel.Error, this);
                    OnDisconnect();

                }

                await Task.CompletedTask;

            } catch (SocketException) {

                OnMajorSocketExceptionWhileConnecting();
                isConnectingStage = false;

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