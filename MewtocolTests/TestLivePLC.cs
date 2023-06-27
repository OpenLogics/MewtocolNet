using MewtocolNet;
using MewtocolNet.Logging;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.Registers;
using MewtocolTests.EncapsulatedTests;
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
                Type = CpuType.FP_Sigma_X_H_30K_60K_120K,
                ProgCapacity = 32,

            },
            new ExpectedPlcInformationData {

                PLCName = "FPX-H C14R",
                PLCIP = "192.168.115.212",
                PLCPort = 9094,
                Type = CpuType.FP_Sigma_X_H_30K_60K_120K,
                ProgCapacity = 16,

            },

        };

        private List<RegisterReadWriteTest> testRegisterRW = new() {

            new RegisterReadWriteTest {
                TargetRegister = new BoolRegister(IOType.R, 0xA, 10),
                RegisterPlcAddressName = "R10A",
                IntialValue = false,
                AfterWriteValue = true,
            },
            new RegisterReadWriteTest {
                TargetRegister = new NumberRegister<int>(3000),
                RegisterPlcAddressName = "DT3000",
                IntialValue = (int)0,
                AfterWriteValue = (int)-513,
            },

        };

        public TestLivePLC(ITestOutputHelper output) {

            this.output = output;

            Logger.LogLevel = LogLevel.Verbose;
            Logger.OnNewLogMessage((d, l, m) => {

                output.WriteLine($"Mewtocol Logger: {d} {m}");

            });

        }

        [Fact(DisplayName = "Connection cycle client to PLC")]
        public async void TestClientConnection() {

            foreach (var plc in testPlcInformationData) {

                output.WriteLine($"Testing: {plc.PLCName}");

                var cycleClient = new MewtocolInterface(plc.PLCIP, plc.PLCPort);

                await cycleClient.ConnectAsync();

                Assert.True(cycleClient.IsConnected);

                cycleClient.Disconnect();

                Assert.False(cycleClient.IsConnected);

            }

        }

        [Fact(DisplayName = "Reading basic information from PLC")]
        public async void TestClientReadPLCStatus() {

            foreach (var plc in testPlcInformationData) {

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

        //[Fact(DisplayName = "Reading basic information from PLC")]
        //public async void TestRegisterReadWriteAsync () {

        //    foreach (var plc in testPlcInformationData) {

        //        output.WriteLine($"Testing: {plc.PLCName}\n");

        //        var client = new MewtocolInterface(plc.PLCIP, plc.PLCPort);

        //        foreach (var testRW in testRegisterRW) {

        //            client.AddRegister(testRW.TargetRegister);

        //        }  

        //        await client.ConnectAsync();
        //        Assert.True(client.IsConnected);

        //        foreach (var testRW in testRegisterRW) {

        //            client.AddRegister(testRW.TargetRegister);

        //        }

        //        Assert.Equal(client.PlcInfo.CpuInformation.Cputype, plc.Type);
        //        Assert.Equal(client.PlcInfo.CpuInformation.ProgramCapacity, plc.ProgCapacity);

        //        client.Disconnect();

        //    }

        //}

    }

}
