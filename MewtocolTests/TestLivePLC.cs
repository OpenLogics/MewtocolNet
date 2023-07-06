﻿using MewtocolNet;
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
                IntialValue = false,
                AfterWriteValue = true,
            },
            new RegisterReadWriteTest {
                TargetRegister = new NumberRegister<short>(3000),
                RegisterPlcAddressName = "DT3000",
                IntialValue = (short)0,
                AfterWriteValue = (short)-513,
            },

        };

        public TestLivePLC(ITestOutputHelper output) {

            this.output = output;

            Logger.LogLevel = LogLevel.Critical;
            Logger.OnNewLogMessage((d, l, m) => {

                output.WriteLine($"Mewtocol Logger: {d} {m}");

            });

        }

        [Fact(DisplayName = "Connection cycle client to PLC")]
        public async void TestClientConnection() {

            foreach (var plc in testPlcInformationData) {

                output.WriteLine($"Testing: {plc.PLCName}");

                var cycleClient = Mewtocol.Ethernet(plc.PLCIP, plc.PLCPort);

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

                var client = Mewtocol.Ethernet(plc.PLCIP, plc.PLCPort);

                await client.ConnectAsync();

                output.WriteLine($"{client.PlcInfo}\n");

                Assert.True(client.IsConnected);

                Assert.Equal(client.PlcInfo.TypeCode, plc.Type);
                Assert.Equal(client.PlcInfo.ProgramCapacity, plc.ProgCapacity);

                client.Disconnect();

            }

        }

        [Fact(DisplayName = "Reading basic information from PLC")]
        public async void TestRegisterReadWriteAsync() {

            foreach (var plc in testPlcInformationData) {

                output.WriteLine($"Testing: {plc.PLCName}\n");

                var client = Mewtocol.Ethernet(plc.PLCIP, plc.PLCPort);

                foreach (var testRW in testRegisterRW) {

                    client.AddRegister(testRW.TargetRegister);

                }

                await client.ConnectAsync();
                Assert.True(client.IsConnected);

                foreach (var testRW in testRegisterRW) {

                    var testRegister = client.Registers.First(x => x.PLCAddressName == testRW.RegisterPlcAddressName);

                    //test inital val
                    Assert.Equal(testRW.IntialValue, testRegister.Value);

                    await testRegister.WriteAsync(testRW.AfterWriteValue);

                    //test after write val
                    Assert.Equal(testRW.AfterWriteValue, testRegister.Value);

                }

                client.Disconnect();

            }

        }

    }

}
