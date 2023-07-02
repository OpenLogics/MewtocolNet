using MewtocolNet;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System.Collections;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests {

    public class AutomatedPropertyRegisters {

        private readonly ITestOutputHelper output;

        public AutomatedPropertyRegisters(ITestOutputHelper output) {
            this.output = output;
        }

        public class TestRegisterCollection : RegisterCollection {

            //corresponds to a R100 boolean register in the PLC
            //can also be written as R1000 because the last one is a special address
            [Register(IOType.R, memoryArea: 85, spAdress: 0)]
            public bool TestBool1 { get; private set; }

            //corresponds to a XD input of the PLC
            [Register(IOType.X, (byte)0xD)]
            public bool TestBoolInputXD { get; private set; }

            //corresponds to a DT1101 - DT1104 string register in the PLC with (STRING[4])
            //[Register(1101, 4)]
            //public string TestString1 { get; private set; }

            //corresponds to a DT7000 16 bit int register in the PLC
            [Register(899)]
            public short TestInt16 { get; private set; }

            [Register(342)]
            public ushort TestUInt16 { get; private set; }

            //corresponds to a DTD7001 - DTD7002 32 bit int register in the PLC
            [Register(7001)]
            public int TestInt32 { get; private set; }

            [Register(765)]
            public uint TestUInt32 { get; private set; }

            //corresponds to a DTD7001 - DTD7002 32 bit float register in the PLC (REAL)
            [Register(7003)]
            public float TestFloat32 { get; private set; }

            //corresponds to a DT7005 - DT7009 string register in the PLC with (STRING[5])
            [Register(7005, 5)]
            public string TestString2 { get; private set; }

            //corresponds to a DT7010 as a 16bit word/int and parses the word as single bits
            [Register(7010)]
            public BitArray TestBitRegister { get; private set; }

            [Register(8010, BitCount.B32)]
            public BitArray TestBitRegister32 { get; private set; }

            //corresponds to a DT1204 as a 16bit word/int takes the bit at index 9 and writes it back as a boolean
            [Register(1204, BitCount.B16, 9)]
            public bool BitValue { get; private set; }

            [Register(1204, BitCount.B32, 5)]
            public bool FillTest { get; private set; }

            //corresponds to a DT7012 - DT7013 as a 32bit time value that gets parsed as a timespan (TIME)
            //the smallest value to communicate to the PLC is 10ms
            [Register(7012)]
            public TimeSpan TestTime { get; private set; }

            public enum CurrentState {
                Undefined = 0,
                State1 = 1,
                State2 = 2,
                //State3 = 3,
                State4 = 4,
                State5 = 5,
                StateBetween = 100,
                State6 = 6,
                State7 = 7,
            }

            [Register(50)]
            public CurrentState TestEnum16 { get; private set; }

            [Register(51, BitCount.B32)]
            public CurrentState TestEnum32 { get; private set; }

        }

        private void TestBasicGeneration(IRegisterInternal reg, string propName, object expectValue, int expectAddr, string expectPlcName) {

            Assert.NotNull(reg);
            Assert.Equal(propName, reg.Name);
            Assert.Equal(expectValue, reg.Value);

            Assert.Equal(expectAddr, reg.MemoryAddress);
            Assert.Equal(expectPlcName, reg.GetRegisterPLCName());

            output.WriteLine(reg.ToString());

        }

        //actual tests

        [Fact(DisplayName = "Boolean R generation")]
        public void BooleanGen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestRegisterCollection()).WithPoller();

            var register = interf.GetRegister(nameof(TestRegisterCollection.TestBool1));

            //test generic properties
            TestBasicGeneration((IRegisterInternal)register, nameof(TestRegisterCollection.TestBool1), false, 85, "R85");

        }

        [Fact(DisplayName = "Boolean input XD generation")]
        public void BooleanInputGen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestRegisterCollection()).WithPoller();

            var register = interf.GetRegister(nameof(TestRegisterCollection.TestBoolInputXD));

            //test generic properties
            TestBasicGeneration((IRegisterInternal)register, nameof(TestRegisterCollection.TestBoolInputXD), false, 0, "XD");

        }

        [Fact(DisplayName = "Int16 generation")]
        public void Int16Gen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestRegisterCollection()).WithPoller();

            var register = interf.GetRegister(nameof(TestRegisterCollection.TestInt16));

            //test generic properties
            TestBasicGeneration((IRegisterInternal)register, nameof(TestRegisterCollection.TestInt16), (short)0, 899, "DT899");

        }

        [Fact(DisplayName = "UInt16 generation")]
        public void UInt16Gen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestRegisterCollection()).WithPoller();

            var register = interf.GetRegister(nameof(TestRegisterCollection.TestUInt16));

            //test generic properties
            TestBasicGeneration((IRegisterInternal)register, nameof(TestRegisterCollection.TestUInt16), (ushort)0, 342, "DT342");

        }

        [Fact(DisplayName = "Int32 generation")]
        public void Int32Gen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestRegisterCollection()).WithPoller();

            var register = interf.GetRegister(nameof(TestRegisterCollection.TestInt32));

            //test generic properties
            TestBasicGeneration((IRegisterInternal)register, nameof(TestRegisterCollection.TestInt32), (int)0, 7001, "DDT7001");

        }

        [Fact(DisplayName = "UInt32 generation")]
        public void UInt32Gen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestRegisterCollection()).WithPoller();

            var register = interf.GetRegister(nameof(TestRegisterCollection.TestUInt32));

            //test generic properties
            TestBasicGeneration((IRegisterInternal)register, nameof(TestRegisterCollection.TestUInt32), (uint)0, 765, "DDT765");

        }

        [Fact(DisplayName = "Float32 generation")]
        public void Float32Gen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestRegisterCollection()).WithPoller();

            var register = interf.GetRegister(nameof(TestRegisterCollection.TestFloat32));

            //test generic properties
            TestBasicGeneration((IRegisterInternal)register, nameof(TestRegisterCollection.TestFloat32), (float)0, 7003, "DDT7003");

        }

        [Fact(DisplayName = "TimeSpan generation")]
        public void TimespanGen() {

            var interf = Mewtocol.Ethernet("192.168.0.1");
            interf.AddRegisterCollection(new TestRegisterCollection()).WithPoller();

            var register = interf.GetRegister(nameof(TestRegisterCollection.TestTime));

            //test generic properties
            TestBasicGeneration((IRegisterInternal)register, nameof(TestRegisterCollection.TestTime), TimeSpan.Zero, 7012, "DDT7012");

        }

    }

}