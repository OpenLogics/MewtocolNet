using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.RegisterAttributes {

    public enum BitCount {
        B16,
        B32
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RegisterAttribute : Attribute {

        public int MemoryArea;
        public int StringLength;
        public RegisterType RegisterType;
        public SpecialAddress SpecialAddress = SpecialAddress.None;
        public BitCount BitCount;
        public int AssignedBitIndex = -1;


        public RegisterAttribute (int memoryArea, int stringLength = 1) {

            MemoryArea = memoryArea;
            StringLength = stringLength;   

        }

        public RegisterAttribute (int memoryArea, RegisterType type) {

            if (type.ToString().StartsWith("DT"))
                throw new NotSupportedException("DT types are not supported for attribute register setups because the number type is casted automatically");

            MemoryArea = memoryArea;
            RegisterType = type;
            SpecialAddress = SpecialAddress.None;

        }

        public RegisterAttribute (RegisterType type, SpecialAddress spAdress) {

            if (type.ToString().StartsWith("DT"))
                throw new NotSupportedException("DT types are not supported for attribute register setups because the number type is casted automatically");

            RegisterType = type;
            SpecialAddress = spAdress;

        }


        public RegisterAttribute (int memoryArea, BitCount bitcount) {

            MemoryArea = memoryArea;
            StringLength = 0;
            BitCount = bitcount;

        }

        public RegisterAttribute (int memoryArea, uint assignBit, BitCount bitcount) {

            if(assignBit > 15 && bitcount == BitCount.B16) {
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
