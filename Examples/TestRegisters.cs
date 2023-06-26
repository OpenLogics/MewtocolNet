using MewtocolNet;
using MewtocolNet.RegisterAttributes;
using System;
using System.Collections;

namespace Examples {
    public class TestRegisters : RegisterCollectionBase {

        //corresponds to a R100 boolean register in the PLC
        [Register(IOType.R, 1000)]
        public bool TestBool1 { get; private set; }

        private int testDuplicate;

        [Register(1000)]
        public int TestDuplicate { 
            get => testDuplicate;
            set => AutoSetter(value, ref testDuplicate);
        }

        //corresponds to a XD input of the PLC
        [Register(IOType.X, (byte)0xD)]
        public bool TestBoolInputXD { get; private set; } 

        //corresponds to a DT1101 - DT1104 string register in the PLC with (STRING[4])
        //[Register(1101, 4)]
        //public string TestString1 { get; private set; }

        //corresponds to a DT7000 16 bit int register in the PLC
        [Register(899)]
        public short TestInt16 { get; private set; }

        //corresponds to a DTD7001 - DTD7002 32 bit int register in the PLC
        [Register(7001)]
        public int TestInt32 { get; private set; }

        //corresponds to a DTD7001 - DTD7002 32 bit float register in the PLC (REAL)
        [Register(7003)]
        public float TestFloat32 { get; private set; }

        //corresponds to a DT7005 - DT7009 string register in the PLC with (STRING[5])
        [Register(7005, 5)]
        public string TestString2 { get; private set; }

        //corresponds to a DT7010 as a 16bit word/int and parses the word as single bits
        [Register(7010)]
        public BitArray TestBitRegister { get; private set; }

        //corresponds to a DT1204 as a 16bit word/int takes the bit at index 9 and writes it back as a boolean
        [Register(1204, BitCount.B16, 9)]
        public bool BitValue { get; private set; }

        [Register(1204, BitCount.B16, 5)]
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
        public CurrentState TestEnum { get; private set; }

        [Register(100)]
        public TimeSpan TsTest2 { get; private set; }

    }
}
