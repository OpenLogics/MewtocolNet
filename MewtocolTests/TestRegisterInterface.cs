using MewtocolNet;
using MewtocolNet.Registers;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests {

    public class TestRegisterInterface {

        private readonly ITestOutputHelper output;

        public TestRegisterInterface(ITestOutputHelper output) {
            this.output = output;
        }

        [Fact(DisplayName = "Numeric mewtocol query building")]
        public void NumericRegisterMewtocolIdentifiers() {

            List<IRegister> registers = new List<IRegister> {
                new NRegister<short>(50, _name: null),
                new NRegister<ushort>(50, _name: null),
                new NRegister<int>(50, _name : null),
                new NRegister<uint>(50, _name : null),
                new NRegister<float>(50, _name : null),
                new NRegister<TimeSpan>(50, _name : null),
            };

            List<string> expectedIdents = new List<string> {
                "D0005000050", //single word register
                "D0005000050", //single word register
                "D0005000051", //double word register
                "D0005000051", //double word register
                "D0005000051", //double word register
                "D0005000051", //double word register
            };

            //test mewtocol idents
            for (int i = 0; i < registers.Count; i++) {

                IRegister? reg = registers[i];
                string expect = expectedIdents[i];

                Assert.Equal(expect, reg.BuildMewtocolQuery());

            }

        }

        [Fact(DisplayName = "PLC register naming convention test")]
        public void PLCRegisterIdentifiers() {

            List<IRegister> registers = new List<IRegister> {
                //numeric ones
                new NRegister<short>(50, _name: null),
                new NRegister<ushort>(60, _name : null),
                new NRegister<int>(70, _name : null),
                new NRegister<uint>(80, _name : null),
                new NRegister<float>(90, _name : null),
                new NRegister<TimeSpan>(100, _name : null),
                
                //boolean
                new BRegister(IOType.R, 0, 100),
                new BRegister(IOType.R, 0, 0),
                new BRegister(IOType.X, 5),
                new BRegister(IOType.X, 0xA),
                new BRegister(IOType.X, 0xF, 109),
                new BRegister(IOType.Y, 0xC, 75),

                //string
                new SRegister(999, 5),
            };

            List<string> expcectedIdents = new List<string> {
                
                //numeric ones
                "DT50",
                "DT60",
                "DDT70",
                "DDT80",
                "DDT90",
                "DDT100",

                //boolean
                "R100",
                "R0",
                "X5",
                "XA",
                "X109F",
                "Y75C",

                 //string
                "DT999"

            };

            //test mewtocol idents
            for (int i = 0; i < registers.Count; i++) {

                IRegister? reg = registers[i];
                string expect = expcectedIdents[i];

                Assert.Equal(expect, reg.GetRegisterPLCName());

            }

        }

        [Fact(DisplayName = "Non allowed (Overflow address)")]
        public void OverFlowRegisterAddress() {

            var ex = Assert.Throws<NotSupportedException>(() => {

                new NRegister<short>(100000, _name: null);

            });

            output.WriteLine(ex.Message.ToString());

            var ex1 = Assert.Throws<NotSupportedException>(() => {

                new BRegister(IOType.R, _areaAdress: 512);

            });

            output.WriteLine(ex1.Message.ToString());

            var ex2 = Assert.Throws<NotSupportedException>(() => {

                new BRegister(IOType.X, _areaAdress: 110);

            });

            output.WriteLine(ex2.Message.ToString());

            var ex3 = Assert.Throws<NotSupportedException>(() => {

                new SRegister(100000, 5);

            });

            output.WriteLine(ex3.Message.ToString());

        }

        [Fact(DisplayName = "Non allowed (Wrong data type)")]
        public void WrongDataTypeRegister() {

            var ex = Assert.Throws<NotSupportedException>(() => {

                new NRegister<double>(100, _name: null);

            });

            output.WriteLine(ex.Message.ToString());

        }

    }

}