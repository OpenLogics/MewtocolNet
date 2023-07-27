using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

//WARNING! The whole UDP protocol was reverse engineered and is not fully implemented..

namespace MewtocolNet.ComCassette {

    /// <summary>
    /// Information about the COM cassette
    /// </summary>
    public class CassetteInformation {

        /// <summary>
        /// Indicates if the cassette is currently configurating
        /// </summary>
        public bool IsConfigurating { get; private set; }

        /// <summary>
        /// Name of the COM cassette
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates the usage of DHCP
        /// </summary>
        public bool UsesDHCP { get; set; }

        /// <summary>
        /// IP Address of the COM cassette
        /// </summary>
        public IPAddress IPAddress { get; set; }

        /// <summary>
        /// Subnet mask of the cassette
        /// </summary>
        public IPAddress SubnetMask { get; set; }

        /// <summary>
        /// Default gateway of the cassette
        /// </summary>
        public IPAddress GatewayAddress { get; set; }

        /// <summary>
        /// Mac address of the cassette
        /// </summary>
        public byte[] MacAddress { get; private set; }

        /// <summary>
        /// Mac address of the cassette formatted as a MAC string (XX:XX:XX:XX:XX) 
        /// </summary>
        public string MacAddressStr => MacAddress.ToHexString(":");

        /// <summary>
        /// The source endpoint the cassette is reachable from
        /// </summary>
        public IPEndPoint Endpoint { get; private set; }

        /// <summary>
        /// The name of the endpoint the device is reachable from, or null if not specifically defined
        /// </summary>
        public string EndpointName { get; private set; }

        /// <summary>
        /// Firmware version as string
        /// </summary>
        public string FirmwareVersion { get; private set; }

        /// <summary>
        /// The tcp port of the cassette
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Status of the cassette
        /// </summary>
        public CassetteStatus Status { get; private set; }

        internal static CassetteInformation FromBytes(byte[] bytes, IPEndPoint endpoint, string endpointName) {

            // Receive data package explained:
            // 0        3    4             8             12                 17               22      24           27        29        31         32
            // 88 C0 00 | 00 | C0 A8 73 D4 | FF FF FF 00 | C0 A8 73 3C | 00 | C0 8F 60 53 1C | 01 10 | 23 86 | 00 | 25 | 00 | 00 | 00 | 0D       | (byte) * (n) NAME LEN
            // Header   |DHCP| IPv4 addr.  | Subnet Mask | IPv4 Gatwy  |    | Mac Addr.      | Ver.  | Port  |    |    |    |STAT|    | Name LEN | Name
            //          1 or 0                                                                                 Procuct Type?  StatusCode Length of Name

            //get ips / mac
            var dhcpOn = bytes.Skip(3).First() != 0x00;
            var ipAdd = new IPAddress(bytes.Skip(4).Take(4).ToArray());
            var subnetMask = new IPAddress(bytes.Skip(8).Take(4).ToArray());
            var gateWaysAdd = new IPAddress(bytes.Skip(12).Take(4).ToArray());
            var macAdd = bytes.Skip(17).Take(5).ToArray();
            var firmwareV = string.Join(".", bytes.Skip(22).Take(2).Select(x => x.ToString("X1")).ToArray());
            var port = BitConverter.ToUInt16(bytes.Skip(24).Take(2).Reverse().ToArray(), 0);
            var status = (CassetteStatus)bytes.Skip(29).First();

            //missing blocks, later

            //get name
            var name = Encoding.ASCII.GetString(bytes.Skip(32).ToArray());

            return new CassetteInformation {

                Name = name,
                UsesDHCP = dhcpOn,
                IPAddress = ipAdd,
                SubnetMask = subnetMask,
                GatewayAddress = gateWaysAdd,
                MacAddress = macAdd,
                Endpoint = endpoint,
                EndpointName = endpointName,
                FirmwareVersion = firmwareV,
                Port = port,
                Status = status,

            };

        }

        public async Task SendNewConfigAsync() {

            if (IsConfigurating) return;

            // this command gets sent to a specific plc ip address to overwrite the cassette config
            // If dhcp is set to 1 the ip is ignored but still must be valid

            // 88 41 00 | 00 | C0 8F 61 07 1B | 05 | 54 65 73 74 31 | 05 | 46 50 58 45 54 | 00 | C0 A8 01 07 | FF FF FF 00 | C0 A8 73 3C 
            // Header   |    |                | 5  | T  e  s  t  1  | 05 | F  P  X  E  T  |0||1| 192.168.1.7 | 255.255...  | 192.168.115.60
            // Header   |    | Mac Address    |LEN>| ASCII Name     |LEN>| Static         |DHCP| Target IP   | Subnet Mask | Gateway

            IsConfigurating = true;

            List<byte> sendBytes = new List<byte>();

            //add cmd header
            sendBytes.AddRange(new byte[] { 0x88, 0x41, 0x00, 0x00 });

            //add mac
            sendBytes.AddRange(MacAddress);

            //add name length
            sendBytes.Add((byte)Name.Length);

            //add name
            sendBytes.AddRange(Encoding.ASCII.GetBytes(Name));

            //FPXET
            var subname = Encoding.ASCII.GetBytes("TESTFP");

            //add sub name length
            sendBytes.Add((byte)subname.Length);

            //add subname
            sendBytes.AddRange(subname);

            //add dhcp 0 | 1
            sendBytes.Add((byte)(UsesDHCP ? 0x01 : 0x00));

            //add ip address
            sendBytes.AddRange(IPAddress.GetAddressBytes());

            //add subnet mask ip address
            sendBytes.AddRange(SubnetMask.GetAddressBytes());

            //add gateway ip
            sendBytes.AddRange(GatewayAddress.GetAddressBytes());

            var sendBytesArr = sendBytes.ToArray();

            using (var udpClient = new UdpClient()) {

                udpClient.Client.Bind(Endpoint);

                //broadcast packet to all devices (plc specific package)
                await udpClient.SendAsync(sendBytesArr, sendBytesArr.Length, "255.255.255.255", 9090);

            }

        }

        public static bool operator ==(CassetteInformation a, CassetteInformation b) => EqualProps(a, b);

        public static bool operator !=(CassetteInformation a, CassetteInformation b) => !EqualProps(a, b);

        private static bool EqualProps (CassetteInformation a, CassetteInformation b) {

            if (a is null && b is null) return true;
            if (!(a is null) && b is null) return false;
            if (!(b is null) && a is null) return false;

            return a.Name == b.Name &&
                   a.UsesDHCP == b.UsesDHCP &&
                   a.IPAddress.ToString() == b.IPAddress.ToString() &&
                   a.SubnetMask.ToString() == b.SubnetMask.ToString() &&
                   a.GatewayAddress.ToString() == b.GatewayAddress.ToString() &&
                   a.MacAddressStr == b.MacAddressStr &&
                   a.FirmwareVersion == b.FirmwareVersion &&
                   a.Port == b.Port &&
                   a.Status == b.Status;

        }

        public override bool Equals(object obj) {

            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            } else {
                return (CassetteInformation)obj == this;
            }

        }

    }

}
