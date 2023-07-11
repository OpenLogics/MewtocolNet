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

        private List<RegisterReadWriteTest> testRegisterRW = new() {

            new RegisterReadWriteTest {
                TargetRegister = new BoolRegister(IOType.R, 0xA, 10),
                RegisterPlcAddressName = "R10A",
                IntermediateValue = false,
                AfterWriteValue = true,
            },
            new RegisterReadWriteTest {
                TargetRegister = new NumberRegister<short>(3000),
                RegisterPlcAddressName = "DT3000",
                IntermediateValue = (short)0,
                AfterWriteValue = (short)-513,
            },
            new RegisterReadWriteTest {
                TargetRegister = new NumberRegister<CurrentState>(3001),
                RegisterPlcAddressName = "DT3001",
                IntermediateValue = CurrentState.Undefined,
                AfterWriteValue = CurrentState.State4,
            },
            new RegisterReadWriteTest {
                TargetRegister = new NumberRegister<CurrentState32>(3002),
                RegisterPlcAddressName = "DDT3002",
                IntermediateValue = CurrentState32.Undefined,
                AfterWriteValue = CurrentState32.StateBetween,
            },
            new RegisterReadWriteTest {
                TargetRegister = new NumberRegister<TimeSpan>(3004),
                RegisterPlcAddressName = "DDT3004",
                IntermediateValue = TimeSpan.Zero,
                AfterWriteValue = TimeSpan.FromSeconds(11),
            },
            new RegisterReadWriteTest {
                TargetRegister = new NumberRegister<TimeSpan>(3006),
                RegisterPlcAddressName = "DDT3006",
                IntermediateValue = TimeSpan.Zero,
                AfterWriteValue = PlcFormat.ParsePlcTime("T#50m"),
            },
            new RegisterReadWriteTest {
                TargetRegister = new StringRegister(40),
                RegisterPlcAddressName = "DT40",
                IntermediateValue = "Hello",
                AfterWriteValue = "TestV",
            },
            new RegisterReadWriteTest {
                TargetRegister = RegBuilder.Factory.FromPlcRegName("DT3008").AsBits(5).Build(),
                RegisterPlcAddressName = "DT3008",
                IntermediateValue = new BitArray(new bool[] { false, false, false, false, false }),
                AfterWriteValue = new BitArray(new bool[] { false, true, false, false, false }),
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

        [Fact(DisplayName = "Reading / Writing registers from PLC (Ethernet)")]
        public async void TestRegisterReadWriteAsync() {

            Logger.LogLevel = LogLevel.Verbose;
            Logger.OnNewLogMessage((d, l, m) => {

                output.WriteLine($"{d:HH:mm:ss:fff} {m}");

            });

            var plc = testPlcInformationData[0];

            output.WriteLine($"\n\n --- Testing: {plc.PLCName} ---\n");

            var client = Mewtocol.Ethernet(plc.PLCIP, plc.PLCPort).Build();

            foreach (var testRW in testRegisterRW) {

                client.AddRegister(testRW.TargetRegister);

            }

            await client.ConnectAsync();
            Assert.True(client.IsConnected);

            //cycle run mode to reset registers to inital
            await client.SetOperationModeAsync(false);
            await client.SetOperationModeAsync(true);

            foreach (var testRW in testRegisterRW) {

                var testRegister = client.Registers.First(x => x.PLCAddressName == testRW.RegisterPlcAddressName);

                //test inital val
                Assert.Null(testRegister.Value);

                await testRegister.ReadAsync();

                Assert.Equal(testRW.IntermediateValue, testRegister.Value);

                await testRegister.WriteAsync(testRW.AfterWriteValue);
                await testRegister.ReadAsync();

                //test after write val
                Assert.Equal(testRW.AfterWriteValue, testRegister.Value);

            }

            client.Disconnect();

        }

    }

}
