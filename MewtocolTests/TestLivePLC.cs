using MewtocolNet;
using MewtocolNet.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests {

    public class TestLivePLC {

        private readonly ITestOutputHelper output;

        private List<ExpectedTestData> testData = new() {

            new ExpectedTestData {

                PLCName = "FPX-H C30T",
                PLCIP = "192.168.115.210",
                PLCPort = 9094,
                Type = CpuType.FP_Sigma_X_H_30K_60K_120K,
                ProgCapacity = 32,

            },
            new ExpectedTestData {

                PLCName = "FPX-H C14R",
                PLCIP = "192.168.115.212",
                PLCPort = 9094,
                Type = CpuType.FP_Sigma_X_H_30K_60K_120K,
                ProgCapacity = 16,

            },

        };

        public TestLivePLC (ITestOutputHelper output) {

            this.output = output;

            Logger.LogLevel = LogLevel.Verbose;
            Logger.OnNewLogMessage((d,m) => {

                output.WriteLine($"Mewtocol Logger: {d} {m}");

            });
        
        }

        [Fact(DisplayName = "Connection cycle client to PLC")]
        public async void TestClientConnection () {

            foreach (var plc in testData) {

                output.WriteLine($"Testing: {plc.PLCName}");

                var cycleClient = new MewtocolInterface(plc.PLCIP, plc.PLCPort);

                await cycleClient.ConnectAsync();

                Assert.True(cycleClient.IsConnected);

                cycleClient.Disconnect();

                Assert.False(cycleClient.IsConnected);

            }

        }

        [Fact(DisplayName = "Reading basic information from PLC")]
        public async void TestClientReadPLCStatus () {

            foreach (var plc in testData) {

                output.WriteLine($"Testing: {plc.PLCName}\n");

                var client = new MewtocolInterface(plc.PLCIP, plc.PLCPort);

                await client.ConnectAsync();

                output.WriteLine($"{client.PlcInfo}\n");

                Assert.True(client.IsConnected);

                Assert.Equal(client.PlcInfo.CpuInformation.Cputype, plc.Type);
                Assert.Equal(client.PlcInfo.CpuInformation.ProgramCapacity, plc.ProgCapacity);

                client.Disconnect();

            }

        }

    }

    public class ExpectedTestData {

        public string PLCName { get; set; }

        public string PLCIP { get; set; }

        public int PLCPort { get; set; }

        public CpuType Type { get; set; } 

        public int ProgCapacity { get; set; }   

    }

}
