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

            List<IRegisterInternal> registers = new List<IRegisterInternal> {
                new NumberRegister<short>(50),
                new NumberRegister<ushort>(50),
                new NumberRegister<int>(50),
                new NumberRegister<uint>(50),
                new NumberRegister<float>(50),
                new NumberRegister<TimeSpan>(50),
                new BytesRegister(50, 30),
                new BytesRegister(50, 31),
            };

            List<string> expectedIdents = new List<string> {
                "D0005000050", //single word register
                "D0005000050", //single word register
                "D0005000051", //double word register
                "D0005000051", //double word register
                "D0005000051", //double word register
                "D0005000051", //double word register
                "D0005000064", //variable len register even bytes
                "D0005000065", //variable len register odd bytes
            };

            //test mewtocol idents
            for (int i = 0; i < registers.Count; i++) {

                IRegisterInternal? reg = registers[i];
                string expect = expectedIdents[i];

                Assert.Equal(expect, reg.BuildMewtocolQuery());

            }

        }

        [Fact(DisplayName = "PLC register naming convention test")]
        public void PLCRegisterIdentifiers() {

            List<IRegisterInternal> registers = new List<IRegisterInternal> {
                //numeric ones
                new NumberRegister<short>(50, _name: null),
                new NumberRegister<ushort>(60, _name : null),
                new NumberRegister<int>(70, _name : null),
                new NumberRegister<uint>(80, _name : null),
                new NumberRegister<float>(90, _name : null),
                new NumberRegister<TimeSpan>(100, _name : null),
                
                //boolean
                new BoolRegister(IOType.R, 0, 100),
                new BoolRegister(IOType.R, 0, 0),
                new BoolRegister(IOType.X, 5),
                new BoolRegister(IOType.X, 0xA),
                new BoolRegister(IOType.X, 0xF, 109),
                new BoolRegister(IOType.Y, 0xC, 75),

                //string
                new BytesRegister(999, 5),
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

                IRegisterInternal? reg = registers[i];
                string expect = expcectedIdents[i];

                Assert.Equal(expect, reg.GetRegisterPLCName());

            }

        }

        [Fact(DisplayName = "Non allowed (Overflow address)")]
        public void OverFlowRegisterAddress() {

            var ex = Assert.Throws<NotSupportedException>(() => {

                new NumberRegister<short>(100000, _name: null);

            });

            output.WriteLine(ex.Message.ToString());

            var ex1 = Assert.Throws<NotSupportedException>(() => {

                new BoolRegister(IOType.R, _areaAdress: 512);

            });

            output.WriteLine(ex1.Message.ToString());

            var ex2 = Assert.Throws<NotSupportedException>(() => {

                new BoolRegister(IOType.X, _areaAdress: 110);

            });

            output.WriteLine(ex2.Message.ToString());

            var ex3 = Assert.Throws<NotSupportedException>(() => {

                new BytesRegister(100000, 5);

            });

            output.WriteLine(ex3.Message.ToString());

        }

        [Fact(DisplayName = "Non allowed (Wrong data type)")]
        public void WrongDataTypeRegister() {

            var ex = Assert.Throws<NotSupportedException>(() => {

                new NumberRegister<double>(100, _name: null);

            });

            output.WriteLine(ex.Message.ToString());

        }

    }

}