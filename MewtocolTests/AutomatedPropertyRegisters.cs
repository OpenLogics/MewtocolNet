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

        private void Test(IRegisterInternal reg, string propName, uint expectAddr, string expectPlcName) {

            Assert.NotNull(reg);
            Assert.Equal(propName, reg.Name);
            Assert.Null(reg.Value);

            Assert.Equal(expectAddr, reg.MemoryAddress);
            Assert.Equal(expectPlcName, reg.GetMewName());

            output.WriteLine(reg.ToString());

        }

        //actual tests

        [Fact(DisplayName = "Boolean generation")]
        public void BooleanGen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestBoolRegisters());

            var register1 = interf.GetRegister(nameof(TestBoolRegisters.RType));
            var register2 = interf.GetRegister(nameof(TestBoolRegisters.XType));

            var register3 = interf.GetRegister(nameof(TestBoolRegisters.RType_MewString));

            Test((IRegisterInternal)register1, nameof(TestBoolRegisters.RType), 85, "R85A");
            Test((IRegisterInternal)register2, nameof(TestBoolRegisters.XType), 0, "XD");

            Test((IRegisterInternal)register3, nameof(TestBoolRegisters.RType_MewString), 85, "R85B");

        }

        [Fact(DisplayName = "Number 16 bit generation")]
        public void N16BitGen () {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new Nums16Bit());

            var register1 = interf.GetRegister(nameof(Nums16Bit.Int16Type));
            var register2 = interf.GetRegister(nameof(Nums16Bit.UInt16Type));
            var register3 = interf.GetRegister(nameof(Nums16Bit.Enum16Type));

            var register4 = interf.GetRegister(nameof(Nums16Bit.Int16Type_MewString));
            var register5 = interf.GetRegister(nameof(Nums16Bit.Enum16Type_MewString));

            //test generic properties
            Test((IRegisterInternal)register1, nameof(Nums16Bit.Int16Type), 899, "DT899");
            Test((IRegisterInternal)register2, nameof(Nums16Bit.UInt16Type), 342, "DT342");
            Test((IRegisterInternal)register3, nameof(Nums16Bit.Enum16Type), 50, "DT50");

            Test((IRegisterInternal)register4, nameof(Nums16Bit.Int16Type_MewString), 900, "DT900");
            Test((IRegisterInternal)register5, nameof(Nums16Bit.Enum16Type_MewString), 51, "DT51");

        }

        [Fact(DisplayName = "Number 32 bit generation")]
        public void N32BitGen () {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new Nums32Bit());

            var register1 = interf.GetRegister(nameof(Nums32Bit.Int32Type));
            var register2 = interf.GetRegister(nameof(Nums32Bit.UInt32Type));
            var register3 = interf.GetRegister(nameof(Nums32Bit.Enum32Type));
            var register4 = interf.GetRegister(nameof(Nums32Bit.FloatType));
            var register5 = interf.GetRegister(nameof(Nums32Bit.TimeSpanType));

            var register6 = interf.GetRegister(nameof(Nums32Bit.Enum32Type_MewString));
            var register7 = interf.GetRegister(nameof(Nums32Bit.TimeSpanType_MewString));

            //test generic properties
            Test((IRegisterInternal)register1, nameof(Nums32Bit.Int32Type), 7001, "DDT7001");
            Test((IRegisterInternal)register2, nameof(Nums32Bit.UInt32Type), 765, "DDT765");
            Test((IRegisterInternal)register3, nameof(Nums32Bit.Enum32Type), 51, "DDT51");
            Test((IRegisterInternal)register4, nameof(Nums32Bit.FloatType), 7003, "DDT7003");
            Test((IRegisterInternal)register5, nameof(Nums32Bit.TimeSpanType), 7012, "DDT7012");

            Test((IRegisterInternal)register6, nameof(Nums32Bit.Enum32Type_MewString), 53, "DDT53");
            Test((IRegisterInternal)register7, nameof(Nums32Bit.TimeSpanType_MewString), 7014, "DDT7014");

        }

        [Fact(DisplayName = "String generation")]
        public void StringGen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestStringRegisters());

            var register1 = interf.GetRegister(nameof(TestStringRegisters.StringType));
            var register2 = interf.GetRegister(nameof(TestStringRegisters.StringType_MewString));

            //test generic properties
            Test((IRegisterInternal)register1, nameof(TestStringRegisters.StringType), 7005, "DT7005");
            Test((IRegisterInternal)register2, nameof(TestStringRegisters.StringType_MewString), 7050, "DT7050");

        }

    }

}