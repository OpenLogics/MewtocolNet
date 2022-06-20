using MewtocolNet;
using MewtocolNet.RegisterAttributes;
using System;
using System.Collections;

namespace Examples {
    public class TestRegisters : RegisterCollectionBase {

        //corresponds to a R100 boolean register in the PLC
        [Register(100, RegisterType.R)]
        public bool TestBool1 { get; private set; }

        //corresponds to a R100 boolean register in the PLC
        [Register(RegisterType.X, SpecialAddress.D)]
        public bool TestBoolInputXD { get; private set; } 

        //corresponds to a DT1101 - DT1104 string register in the PLC with (STRING[4])
        [Register(1101, 4)]
        public string TestString1 { get; private set; }

        //corresponds to a DT7000 16 bit int register in the PLC
        [Register(7000)]
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
        [Register(1204, 9, BitCount.B16)]
        public bool BitValue { get; private set; }

        //corresponds to a DT7012 - DT7013 as a 32bit time value that gets parsed as a timespan (TIME)
        //the smallest value to communicate to the PLC is 10ms
        [Register(7012)]
        public TimeSpan TestTime { get; private set; }  


    }
}
