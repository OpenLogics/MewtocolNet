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

        [Fact(DisplayName = "Non allowed Struct Address (Overflow address)")]
        public void OverFlowStructRegister() {

            var ex = Assert.Throws<NotSupportedException>(() => {

                new StructRegister<short>(100000, 2);

            });

            output.WriteLine(ex.Message.ToString());

        }

        [Fact(DisplayName = "Non allowed Boolean Address (Overflow address )")]
        public void OverFlowBoolRegister() {

            var ex1 = Assert.Throws<NotSupportedException>(() => {

                new BoolRegister(IOType.R, _areaAdress: 512);

            });

            output.WriteLine(ex1.Message.ToString());

            var ex2 = Assert.Throws<NotSupportedException>(() => {

                new BoolRegister(IOType.X, _areaAdress: 110);

            });

            output.WriteLine(ex2.Message.ToString());

        }

    }

}