using MewtocolNet.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
        public IPEndPoint HostEndpoint { get; internal set; }

        internal MewtocolInterfaceTcp() : base() { }

        #region TCP connection state handling

        /// <inheritdoc/>
        public void ConfigureConnection(string ip, int port = 9094, int station = 0xEE) {

            if (IsConnected)
                throw new NotSupportedException("Can't change the connection settings while the PLC is connected");

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

            if (IsConnected)
                throw new NotSupportedException("Can't change the connection settings while the PLC is connected");

            ipAddr = ip;
            Port = port;

            if (stationNumber != 0xEE && stationNumber > 99)
                throw new NotSupportedException("Station number can't be greater than 99");

            stationNumber = station;

            Disconnect();

        }

        /// <inheritdoc/>
        public override async Task ConnectAsync(Func<Task> callBack = null) => await ConnectAsyncPriv(callBack);

        private void BuildTcpClient () {

            if (HostEndpoint != null) {

                var hasEndpoint = Mewtocol
                .GetSourceEndpoints()
                .Any(x => x.Address.ToString() == HostEndpoint.Address.ToString());

                if (!hasEndpoint)
                    throw new NotSupportedException($"The specified source endpoint: " +
                                                    $"{HostEndpoint}, doesn't exist on the device, " +
                                                    $"use 'Mewtocol.GetSourceEndpoints()' to find applicable ones");

                client = new TcpClient(HostEndpoint) {
                    ReceiveBufferSize = RecBufferSize,
                    NoDelay = false,
                    ReceiveTimeout = sendReceiveTimeoutMs,
                    SendTimeout = sendReceiveTimeoutMs,
                };

                var ep = (IPEndPoint)client.Client.LocalEndPoint;
                Logger.Log($"Connecting [MAN] endpoint: {ep.Address}:{ep.Port}", LogLevel.Info, this);

            } else {

                client = new TcpClient() {
                    ReceiveBufferSize = RecBufferSize,
                    NoDelay = false,
                    ReceiveTimeout = sendReceiveTimeoutMs,
                    SendTimeout = sendReceiveTimeoutMs,
                };

            }

        }

        private async Task ConnectAsyncPriv(Func<Task> callBack = null) {

            try {

                firstPollTask = new Task(() => { });

                Logger.Log($">> Intial connection start <<", LogLevel.Verbose, this);
                isConnectingStage = true;

                BuildTcpClient();

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
                var plcinf = await GetPLCInfoAsync(ConnectTimeout);

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

        protected override async Task ReconnectAsync (int conTimeout) {

            try {

                firstPollTask = new Task(() => { });

                Logger.Log($">> Reconnect start <<", LogLevel.Verbose, this);

                isConnectingStage = true;

                BuildTcpClient();

                var result = client.BeginConnect(ipAddr, Port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(conTimeout));

                if (client.Connected)
                    Logger.LogVerbose("TCP/IP Client connected", this);

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

                Logger.LogVerbose("Attached stream, getting PLC info", this);

                //get plc info
                var plcinf = await GetPLCInfoAsync(ConnectTimeout);

                if (plcinf != null) {

                    IsConnected = true;
                    await base.ConnectAsync();

                    Logger.LogVerbose("Connection re-established", this);
                    OnConnected(plcinf);

                } else {

                    Logger.Log("Initial connection failed", LogLevel.Error, this);
                    OnDisconnect();

                }

                await Task.CompletedTask;

            } catch (Exception ex) {

                Logger.LogError($"Reconnect exception: {ex.Message}");

            }

        }

        /// <summary>
        /// Gets the connection info string
        /// </summary>
        public override string GetConnectionInfo() {

            return $"{IpAddress}:{Port}";

        }

        private protected override void OnDisconnect() {

            base.OnDisconnect();

            if (client != null && client.Connected) {

                client.Close();

            }

        }

        #endregion

    }

}