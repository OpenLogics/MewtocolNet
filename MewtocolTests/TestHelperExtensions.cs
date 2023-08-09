using MewtocolNet;
using System.Collections;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests {

    public class TestHelperExtensions {

        private readonly ITestOutputHelper output;

        public TestHelperExtensions(ITestOutputHelper output) {
            this.output = output;
        }

        [Fact(DisplayName = nameof(PlcFormat.ToBitString))]
        public void ToBitStringGeneration() {

            var bitarr = new BitArray(16);
            bitarr[2] = true;
            bitarr[5] = true;
            bitarr[8] = true;
            bitarr[11] = true;
            bitarr[14] = true;

            Assert.Equal("0010010010010010", bitarr.ToBitString());

        }

        [Fact(DisplayName = nameof(MewtocolHelpers.ToHexString))]
        public void ToHexStringGeneration() {

            var bytes = new byte[6] {
                0x10,
                0xAB,
                0xAC,
                0x32,
                0x00,
                0x01
            };

            Assert.Equal("10ABAC320001", bytes.ToHexString());

        }

        [Fact(DisplayName = nameof(MewtocolHelpers.BytesFromHexASCIIString))]
        public void ToHexASCIIBytesGeneration() {

            string test = "Hello, world!";

            Assert.Equal(new byte[] {
                0x48,
                0x45,
                0x4C,
                0x4C,
                0x4F,
                0x2C,
                0x20,
                0x57,
                0x4F,
                0x52,
                0x4C,
                0x44,
                0x21
            }, test.BytesFromHexASCIIString());

        }

        [Fact(DisplayName = nameof(MewtocolHelpers.ParseResponseStringAsBytes))]
        public void ParseDTByteStringGeneration() {

            var testList = new List<byte[]>() {
                new byte[] {0x11, 0x12},
                new byte[] {0x1C, 0x2C},
                new byte[] {0xFF, 0xFF},
            };

            foreach (var item in testList) {

                Assert.Equal(item, $"%01$RD{item.ToHexString()}".BCC_Mew().ParseResponseStringAsBytes());

            }

        }

        [Fact(DisplayName = nameof(MewtocolHelpers.ParseRCSingleBit))]
        public void ParseRCSingleBitGeneration() {

            Assert.True($"%01$RC1".BCC_Mew().ParseRCSingleBit());
            Assert.False($"%01$RC0".BCC_Mew().ParseRCSingleBit());

        }

        [Fact(DisplayName = nameof(PlcFormat.ParsePlcTime))]
        public void ParsePlcTime () {

            Assert.Equal(new TimeSpan(5, 30, 30, 15, 10), PlcFormat.ParsePlcTime("T#5d30h30m15s10ms"));
            Assert.Equal(new TimeSpan(0, 30, 30, 15, 10), PlcFormat.ParsePlcTime("T#30h30m15s10ms"));
            Assert.Equal(new TimeSpan(0, 1, 30, 15, 10), PlcFormat.ParsePlcTime("T#1h30m15s10ms"));
            Assert.Equal(new TimeSpan(0, 0, 5, 30, 10), PlcFormat.ParsePlcTime("T#5m30s10ms"));
            Assert.Throws<NotSupportedException>(() => PlcFormat.ParsePlcTime("T#5m30s5ms"));

        }

        [Fact(DisplayName = nameof(PlcFormat.ToPlcTime))]
        public void ToPlcTime() {

            Assert.Equal("T#1d6h5m30s10ms", PlcFormat.ToPlcTime(new TimeSpan(0, 30, 5, 30, 10)));
            Assert.Equal("T#6d5h30m10s", PlcFormat.ToPlcTime(new TimeSpan(6, 5, 30, 10)));

        }

    }

}