using Xunit;

using MewtocolNet;
using MewtocolNet.Registers;
using Xunit.Abstractions;
using MewtocolNet.Links;
using System.Collections;

namespace MewtocolTests {

    public class TestHelperExtensions {

        private readonly ITestOutputHelper output;

        public TestHelperExtensions (ITestOutputHelper output) {
            this.output = output;
        }

        [Fact(DisplayName = nameof(MewtocolHelpers.ToBitString))]
        public void ToBitStringGeneration () {

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

        [Fact(DisplayName = nameof(MewtocolHelpers.ToHexASCIIBytes))]
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
            }, test.ToHexASCIIBytes());

        }

    }

}