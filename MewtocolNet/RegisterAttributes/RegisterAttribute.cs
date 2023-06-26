using System;

namespace MewtocolNet.RegisterAttributes {

    /// <summary>
    /// Defines the behavior of a register property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegisterAttribute : Attribute {

        internal RegisterType? RegisterType;

        internal int MemoryArea = 0;
        internal int ByteLength = 2;
        internal byte SpecialAddress = 0x0;

        internal BitCount BitCount;
        internal int AssignedBitIndex = -1;

        /// <summary>
        /// Attribute for string type or numeric registers 
        /// </summary>
        /// <param name="memoryArea">The area in the plcs memory</param>
        public RegisterAttribute(int memoryArea) {

            MemoryArea = memoryArea;

        }

        public RegisterAttribute(int memoryArea, int byteLength) {

            MemoryArea = memoryArea;
            ByteLength = byteLength;        

        }

        public RegisterAttribute(int memoryArea, BitCount bitCount) {

            MemoryArea = memoryArea;
            BitCount = bitCount;
            AssignedBitIndex = 0;

            RegisterType = BitCount == BitCount.B16 ? MewtocolNet.RegisterType.DT : MewtocolNet.RegisterType.DDT;

        }

        public RegisterAttribute(int memoryArea, BitCount bitCount, int bitIndex) {

            MemoryArea = memoryArea;
            BitCount = bitCount;
            AssignedBitIndex = bitIndex;

            RegisterType = BitCount == BitCount.B16 ? MewtocolNet.RegisterType.DT : MewtocolNet.RegisterType.DDT;

        }

        /// <summary>
        /// Attribute for boolean registers
        /// </summary>
        public RegisterAttribute(IOType type, byte spAdress = 0x0) {

            MemoryArea = 0;
            RegisterType = (RegisterType)(int)type;
            SpecialAddress = spAdress;

        }

        /// <summary>
        /// Attribute for boolean registers
        /// </summary>
        public RegisterAttribute(IOType type, int memoryArea, byte spAdress = 0x0) {

            MemoryArea = memoryArea;
            RegisterType = (RegisterType)(int)type;
            SpecialAddress = spAdress;

        }

    }

}
