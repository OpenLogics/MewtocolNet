using System;

namespace MewtocolNet.RegisterAttributes {

    /// <summary>
    /// Defines the behavior of a register property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegisterAttribute : Attribute {

        internal int MemoryArea;
        internal int StringLength;
        internal RegisterType RegisterType;
        internal byte SpecialAddress = 0x0;
        internal BitCount BitCount;
        internal int AssignedBitIndex = -1;

        /// <summary>
        /// Attribute for string type or numeric registers 
        /// </summary>
        /// <param name="memoryArea">The area in the plcs memory</param>
        /// <param name="stringLength">The max string length in the plc</param>
        public RegisterAttribute(int memoryArea, int stringLength = 1) {

            MemoryArea = memoryArea;
            StringLength = stringLength;

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

        /// <summary>
        /// Attribute to read numeric registers as bitwise
        /// </summary>
        /// <param name="memoryArea">The area in the plcs memory</param>
        /// <param name="bitcount">The number of bits to parse</param>
        public RegisterAttribute(int memoryArea, BitCount bitcount) {

            MemoryArea = memoryArea;
            StringLength = 0;
            BitCount = bitcount;

        }

        /// <summary>
        /// Attribute to read numeric registers as bitwise
        /// </summary>
        /// <param name="memoryArea">The area in the plcs memory</param>
        /// <param name="bitcount">The number of bits to parse</param>
        /// <param name="assignBit">The index of the bit that gets linked to the bool</param>
        public RegisterAttribute(int memoryArea, uint assignBit, BitCount bitcount) {

            if (assignBit > 15 && bitcount == BitCount.B16) {
                throw new NotSupportedException("The assignBit parameter cannot be greater than 15 in a 16 bit var");
            }

            if (assignBit > 31 && bitcount == BitCount.B32) {
                throw new NotSupportedException("The assignBit parameter cannot be greater than 31 in a 32 bit var");
            }

            MemoryArea = memoryArea;
            StringLength = 0;
            BitCount = bitcount;
            AssignedBitIndex = (int)assignBit;

        }

    }

}
