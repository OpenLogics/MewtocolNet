using MewtocolNet;
using MewtocolNet.Registers;
using MewtocolTests.EncapsulatedTests;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests {

    public partial class AutomatedPropertyRegisters {

        private readonly ITestOutputHelper output;

        public AutomatedPropertyRegisters(ITestOutputHelper output) {
            this.output = output;
        }

        private void Test(IRegisterInternal reg, uint expectAddr, string expectPlcName) {

            Assert.NotNull(reg);
            Assert.StartsWith("auto_prop_register_", reg.Name);
            Assert.Null(reg.Value);

            Assert.Equal(expectAddr, reg.MemoryAddress);
            Assert.Equal(expectPlcName, reg.GetMewName());

            output.WriteLine(reg.ToString());

        }

        //actual tests

        [Fact(DisplayName = "Boolean generation")]
        public void BooleanGen() {

            var interf = Mewtocol.Ethernet("192.168.0.1")
            .WithRegisterCollections(x =>
                x.AddCollection(new TestBoolRegisters())
            ).Build();

            output.WriteLine(((MewtocolInterface)interf).memoryManager.ExplainLayout());

            var register1 = interf.GetRegister("auto_prop_register_1");
            var register2 = interf.GetRegister("auto_prop_register_2");
            var register3 = interf.GetRegister("auto_prop_register_3");

            Test((IRegisterInternal)register1, 0, "XD");
            Test((IRegisterInternal)register2, 85, "R85A");
            Test((IRegisterInternal)register3, 85, "R85B");

        }

        [Fact(DisplayName = "Number 16 bit generation")]
        public void N16BitGen () {

            var interf = Mewtocol.Ethernet("192.168.0.1")
            .WithRegisterCollections(x =>
                x.AddCollection(new Nums16Bit())
            ).Build();

            var register1 = interf.GetRegister("auto_prop_register_1");
            var register2 = interf.GetRegister("auto_prop_register_2");
            var register3 = interf.GetRegister("auto_prop_register_3");

            //test generic properties
            Test((IRegisterInternal)register1, 50, "DT50");
            Test((IRegisterInternal)register2, 342, "DT342");
            Test((IRegisterInternal)register3, 899, "DT899");

        }

        [Fact(DisplayName = "Number 32 bit generation")]
        public void N32BitGen () {

            var interf = Mewtocol.Ethernet("192.168.0.1")
            .WithRegisterCollections(x => x
                .AddCollection(new Nums32Bit())
            ).Build();

            output.WriteLine(((MewtocolInterface)interf).memoryManager.ExplainLayout());

            var register1 = interf.GetRegister("auto_prop_register_1");
            var register2 = interf.GetRegister("auto_prop_register_2");
            var register3 = interf.GetRegister("auto_prop_register_3");
            
            //only one generated because same type
            var register4 = interf.GetRegister("auto_prop_register_4");

            var register6 = interf.GetRegister("auto_prop_register_5");
            var register7 = interf.GetRegister("auto_prop_register_6");

            //test generic properties
            Test((IRegisterInternal)register1, 7000, "DDT7000");
            Test((IRegisterInternal)register2, 7002, "DDT7002");
            Test((IRegisterInternal)register3, 7004, "DDT7004");
            
            Test((IRegisterInternal)register4, 7006, "DDT7006");

            Test((IRegisterInternal)register6, 7008, "DDT7008");
            Test((IRegisterInternal)register7, 7010, "DDT7010");

        }

        [Fact(DisplayName = "String generation")]
        public void StringGen() {

            var interf = Mewtocol.Ethernet("192.168.0.1")
            .WithRegisterCollections(x =>
                x.AddCollection(new TestStringRegisters())
            ).Build();

            var register1 = interf.GetRegister("auto_prop_register_1");

            //test generic properties
            Test((IRegisterInternal)register1, 7005, "DT7005");

        }

        [Fact(DisplayName = "Byte Array generation")]
        public void ByteArrGen() {

            var interf = Mewtocol.Ethernet("192.168.0.1")
            .WithRegisterCollections(x =>
                x.AddCollection(new TestBitwiseRegisters())
            ).Build();

            var register1 = interf.GetRegister("auto_prop_register_1");
            //var register2 = interf.GetRegister("auto_prop_register_2");

            //test generic properties
            Test((IRegisterInternal)register1, 7000, "DT7000");
            //Test((IRegisterInternal)register2, 7001, "DT7001");

        }

    }

}