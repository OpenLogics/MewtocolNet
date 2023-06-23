using MewtocolNet;
using MewtocolNet.RegisterAttributes;
using System;
using System.Collections;

namespace Examples {

    public class TestRegistersEnumBitwise : RegisterCollectionBase {

        private bool startCyclePLC;

        [Register(IOType.R, 50)]
        public bool StartCyclePLC { 
            get => startCyclePLC;
            set => AutoSetter(value, ref startCyclePLC);
        }

        //the enum you want to read out
        public enum CurrentState {

            Undefined = 0,
            State1 = 1,
            State2 = 2,
            //If you leave an enum empty it still works
            //State3 = 3,
            State4 = 4,
            State5 = 5,
            State6 = 6,
            State7 = 7,
            State8 = 8,
            State9 = 9,
            State10 = 10,
            State11 = 11,
            State12 = 12,
            State13 = 13,
            State14 = 14,
            State15 = 15,

        }

        //automatically convert the short (PLC int) register to an enum
        [Register(500)]
        public CurrentState TestEnum16 { get; private set; }

        //also works for 32bit registers
        [Register(501, BitCount.B32)]
        public CurrentState TestEnum32 { get; private set; }

        //get the whole bit array from DT503

        [Register(503)]
        public BitArray TestBitRegister16 { get; private set; }

        //you can also extract single bits from DT503

        [Register(503, 0, BitCount.B16)]
        public bool BitValue0 { get; private set; }

        [Register(503, 1, BitCount.B16)]
        public bool BitValue1 { get; private set; }

        [Register(503, 2, BitCount.B16)]
        public bool BitValue2 { get; private set; }

        [Register(503, 3, BitCount.B16)]
        public bool BitValue3 { get; private set; }

        [Register(503, 4, BitCount.B16)]
        public bool BitValue4 { get; private set; }

        [Register(503, 5, BitCount.B16)]
        public bool BitValue5 { get; private set; }

        [Register(503, 6, BitCount.B16)]
        public bool BitValue6 { get; private set; }

        [Register(503, 7, BitCount.B16)]
        public bool BitValue7 { get; private set; }

        [Register(503, 8, BitCount.B16)]
        public bool BitValue8 { get; private set; }

        [Register(503, 9, BitCount.B16)]
        public bool BitValue9 { get; private set; }

        [Register(503, 10, BitCount.B16)]
        public bool BitValue10 { get; private set; }

        [Register(503, 11, BitCount.B16)]
        public bool BitValue11 { get; private set; }

        [Register(503, 12, BitCount.B16)]
        public bool BitValue12 { get; private set; }

        [Register(503, 13, BitCount.B16)]
        public bool BitValue13 { get; private set; }

        [Register(503, 14, BitCount.B16)]
        public bool BitValue14 { get; private set; }

        [Register(503, 15, BitCount.B16)]
        public bool BitValue15 { get; private set; }

    }

}
