using MewtocolNet;
using MewtocolNet.RegisterAttributes;
using System.Collections;

namespace MewtocolTests.EncapsulatedTests {

    public enum CurrentState16 : short {
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

        [Register("R85A")]
        public bool RType { get; set; }

        [Register("XD")]
        public bool XType { get; set; }

        [Register("R85B")]
        public bool RType_MewString { get; set; }

    }

    public class Nums16Bit : RegisterCollection {


        [Register("DT899")]
        public short Int16Type { get; set; }

        [Register("DT342")]
        public ushort UInt16Type { get; set; }

        [Register("DT50")]
        public CurrentState16 Enum16Type { get; set; }

    }

    public class Nums32Bit : RegisterCollection {

        [Register("DDT7000")]
        public int Int32Type { get; set; }

        [Register("DDT7002")]
        public uint UInt32Type { get; set; }

        [Register("DDT7004")]
        public CurrentState32 Enum32Type { get; set; }

        [Register("DDT7006")]
        public float FloatType { get; set; }

        [Register("DDT7006")]
        public float FloatType2 { get; set; } // this is legal, because the cast type is the same

        //[Register("DDT7006")]
        //public int FloatType3 { get; set; } // this is not legal

        [Register("DDT7010")]
        public TimeSpan TimeSpanType { get; set; }

        [Register("DDT7008")]
        public TimeSpan TimeSpanType2 { get; set; }

        [Register("DDT7013")]
        public TimeSpan TimeSpanType3 { get; set; }

    }

    public class TestStringRegisters : RegisterCollection {

        [Register("DT7005")]
        public string? StringType { get; set; }

    }

    public class TestBitwiseRegisters : RegisterCollection {


        //[Register("DT7001")]
        //public BitArray BitArr32 { get; set; }

    }

}