using MewtocolNet;
using MewtocolNet.RegisterAttributes;
using System.Collections;

namespace MewtocolTests.EncapsulatedTests {

    public enum CurrentState : short {
        Undefined = 0,
        State1 = 1,
        State2 = 2,
        //State3 = 3, <= leave empty for test purposes
        State4 = 4,
        State5 = 5,
        StateBetween = 100,
        State6 = 6,
        State7 = 7,
    }

    public enum CurrentState32 : int {
        Undefined = 0,
        State1 = 1,
        State2 = 2,
        //State3 = 3, <= leave empty for test purposes
        State4 = 4,
        State5 = 5,
        StateBetween = 100,
        State6 = 6,
        State7 = 7,
    }

    public class TestBoolRegisters : RegisterCollection {

        [Register(IOType.R, memoryArea: 85, spAdress: 0xA)]
        public bool RType { get; set; }

        [Register(IOType.X, (byte)0xD)]
        public bool XType { get; set; }

        [Register("R85B")]
        public bool RType_MewString { get; set; }

    }

    public class Nums16Bit : RegisterCollection {


        [Register(899)]
        public short Int16Type { get; set; }

        [Register(342)]
        public ushort UInt16Type { get; set; }

        [Register(50)]
        public CurrentState Enum16Type { get; set; }

        [Register("DT900")]
        public short Int16Type_MewString { get; set; }

        [Register("DT51")]
        public CurrentState Enum16Type_MewString { get; set; }

    }

    public class Nums32Bit : RegisterCollection {

        [Register(7001)]
        public int Int32Type { get; set; }

        [Register(765)]
        public uint UInt32Type { get; set; }

        [Register(51)]
        public CurrentState32 Enum32Type { get; set; }

        [Register(7003)]
        public float FloatType { get; set; }

        [Register(7012)]
        public TimeSpan TimeSpanType { get; set; }

        [Register("DDT53")]
        public CurrentState32 Enum32Type_MewString { get; set; }

        [Register("DDT7014")]
        public TimeSpan TimeSpanType_MewString { get; set; }

    }

    public class TestStringRegisters : RegisterCollection {

        [Register(7005, 5)]
        public string? StringType { get; set; }

        [Register("DT7050")]
        public string? StringType_MewString { get; set; }

    }

    public class TestBitwiseRegisters : RegisterCollection {

        [Register(7010)]
        public BitArray TestBitRegister { get; set; }

        [Register(8010, BitCount.B32)]
        public BitArray TestBitRegister32 { get; set; }

        [Register(1204, BitCount.B16, 9)]
        public bool BitValue { get; set; }

        [Register(1204, BitCount.B32, 5)]
        public bool FillTest { get; set; }

    }

}