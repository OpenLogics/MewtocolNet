using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet.ComCassette {

    /// <summary>
    /// Provides a interface to modify and find PLC network cassettes also known as COM5
    /// </summary>
    public class CassetteFinder {

        public static async Task<IEnumerable<CassetteInformation>> FindClientsAsync(string ipSource = null, int timeoutMs = 100) {

            var from = new IPEndPoint(IPAddress.Any, 0);

            var interfacesTasks = new List<Task<List<CassetteInformation>>>();

            var usableInterfaces = GetUseableNetInterfaces();

            if (ipSource == null) {

                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface netInterface in usableInterfaces) {

                    IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                    var unicastInfo = ipProps.UnicastAddresses
                    .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

                    var ep = new IPEndPoint(unicastInfo.Address, 0);
                    interfacesTasks.Add(FindClientsForEndpoint(ep, timeoutMs, netInterface.Name));

                }

            } else {

                from = new IPEndPoint(IPAddress.Parse(ipSource), 0);

                var netInterface = usableInterfaces.FirstOrDefault(x => x.GetIPProperties().UnicastAddresses.Any(y => y.Address.ToString() == ipSource));

                if (netInterface == null)
                    throw new NotSupportedException($"The host endpoint {ipSource}, is not available");

                interfacesTasks.Add(FindClientsForEndpoint(from, timeoutMs, netInterface.Name));

            }

            //run the interface querys
            var grouped = await Task.WhenAll(interfacesTasks);

            var decomposed = new List<CassetteInformation>();

            foreach (var grp in grouped) {

                foreach (var cassette in grp) {

                    if (decomposed.Any(x => x.MacAddress.SequenceEqual(cassette.MacAddress))) continue;

                    decomposed.Add(cassette);

                }

            }

            return decomposed;

        }

        private static IEnumerable<NetworkInterface> GetUseableNetInterfaces() {

            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces()) {

                bool isEthernet =
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet;

                bool isWlan = netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;

                bool isUsable = netInterface.OperationalStatus == OperationalStatus.Up;

                if (!isUsable) continue;
                if (!(isWlan || isEthernet)) continue;

                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                var hasUnicastInfo = ipProps.UnicastAddresses
                .Any(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

                if (!hasUnicastInfo) continue;

                yield return netInterface;

            }

        }

        private static async Task<List<CassetteInformation>> FindClientsForEndpoint(IPEndPoint from, int timeoutMs, string ipEndpointName) {

            var cassettesFound = new List<CassetteInformation>();

            int plcPort = 9090;

            // Byte msg to request the status transmission of all plcs
            byte[] requestCode = new byte[] { 0x88, 0x40, 0x00 };

            // The start code of the status transmission response
            byte[] startCode = new byte[] { 0x88, 0xC0, 0x00 };

            using (var udpClient = new UdpClient()) {

                udpClient.EnableBroadcast = true;

                udpClient.Client.Bind(from);

                //broadcast packet to all devices (plc specific package)
                udpClient.Send(requestCode, requestCode.Length, "255.255.255.255", plcPort);

                //canceling after no new data was read
                CancellationTokenSource tSource = new CancellationTokenSource();
                var tm = new System.Timers.Timer(timeoutMs);
                tm.Elapsed += (s, e) => {
                    tSource.Cancel();
                    tm.Stop();
                };
                tm.Start();

                //wait for devices to send response
                try {

                    byte[] recvBuffer = null;

                    while (!tSource.Token.IsCancellationRequested) {

                        var res = await udpClient.ReceiveAsync().WithCancellation(tSource.Token);

                        if (res.Buffer == null) break;

                        recvBuffer = res.Buffer;

                        if (recvBuffer.SearchBytePattern(startCode) == 0) {

                            tm.Stop();
                            tm.Start();

                            var parsed = CassetteInformation.FromBytes(recvBuffer, from, ipEndpointName);
                            if (parsed != null) cassettesFound.Add(parsed);

                        }

                    }

                } catch (OperationCanceledException) { } catch (SocketException) { }

            }

            return cassettesFound;

        }

    }

}
