using Xunit;

using MewtocolNet;
using MewtocolNet.Registers;
using System.Diagnostics;
using Xunit.Abstractions;
using System.Collections;

namespace MewtocolTests {

    public class TestComProtocol {

        private readonly ITestOutputHelper output;

        public TestComProtocol (ITestOutputHelper output) {
            this.output = output;
        }

        [Fact(DisplayName = "Numeric register protocol identifiers")]
        public void NumericRegisterMewtocolIdentifiers () {

            List<Register> registers = new List<Register> {
                new NRegister<short>(50),
                new NRegister<ushort>(50),
                new NRegister<int>(50),
                new NRegister<uint>(50),
                new NRegister<float>(50),
            };

            List<string> expcectedIdents = new List<string> {
                "D0005000050", //single word register
                "D0005000050", //single word register
                "D0005000051", //double word register
                "D0005000051", //double word register
                "D0005000051", //double word register
                "D0005000051", //double word register
            };

            //test mewtocol idents
            for (int i = 0; i < registers.Count; i++) {

                Register? reg = registers[i];
                string expect = expcectedIdents[i];

                Assert.Equal(expect, reg.BuildMewtocolIdent());

            }

        }

    }

}