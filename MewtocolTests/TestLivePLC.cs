using MewtocolNet;
using MewtocolNet.Logging;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.Registers;
using MewtocolTests.EncapsulatedTests;
using System.Collections;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests
{

    public class TestLivePLC {

        private readonly ITestOutputHelper output;

        private List<ExpectedPlcInformationData> testPlcInformationData = new() {

            new ExpectedPlcInformationData {

                PLCName = "FPX-H C30T",
                PLCIP = "192.168.115.210",
                PLCPort = 9094,
                Type = PlcType.FPdXH_32k__C30TsP_C40T_C60TsP,
                ProgCapacity = 32,

            },
            new ExpectedPlcInformationData {

                PLCName = "FPX-H C14R",
                PLCIP = "192.168.115.212",
                PLCPort = 9094,
                Type = PlcType.FPdXH_16k__C14R,
                ProgCapacity = 16,

            },

        };

        public TestLivePLC(ITestOutputHelper output) {

            this.output = output;

        }

        [Fact(DisplayName = "Connection cycle client to PLC (Ethernet)")]
        public async void TestClientConnection() {

            foreach (var plc in testPlcInformationData) {

                output.WriteLine($"Testing: {plc.PLCName}");

                var cycleClient = Mewtocol.Ethernet(plc.PLCIP, plc.PLCPort).Build();

                await cycleClient.ConnectAsync();

                Assert.True(cycleClient.IsConnected);

                cycleClient.Disconnect();

                Assert.False(cycleClient.IsConnected);

            }

        }

        [Fact(DisplayName = "Reading basic status from PLC (Ethernet)")]
        public async void TestClientReadPLCStatus() {

            foreach (var plc in testPlcInformationData) {

                output.WriteLine($"Testing: {plc.PLCName}\n");

                var client = Mewtocol.Ethernet(plc.PLCIP, plc.PLCPort).Build();

                await client.ConnectAsync();

                output.WriteLine($"{client.PlcInfo}\n");

                Assert.True(client.IsConnected);

                Assert.Equal(client.PlcInfo.TypeCode, plc.Type);
                Assert.Equal(client.PlcInfo.ProgramCapacity, plc.ProgCapacity);

                client.Disconnect();

            }

        }

    }

}
