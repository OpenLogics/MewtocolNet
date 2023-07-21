using System;
using System.Text;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a boolean
    /// </summary>
    public class BoolRegister : Register {

        internal byte specialAddress;
        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public byte SpecialAddress => specialAddress;

        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public BoolRegister() =>
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");

        internal BoolRegister(SingleBitPrefix _io, byte _spAddress = 0x0, uint _areaAdress = 0, string _name = null) {

            lastValue = null;

            memoryAddress = _areaAdress;
            specialAddress = _spAddress;
            name = _name;

            RegisterType = (RegisterPrefix)(int)_io;

            CheckAddressOverflow(memoryAddress, 0);

        }

        protected override void CheckAddressOverflow(uint addressStart, uint addressLen) {

            if ((int)RegisterType == (int)SingleBitPrefix.R && addressStart >= 512)
                throw new NotSupportedException("R area addresses cant be greater than 511");

            if (((int)RegisterType == (int)SingleBitPrefix.X || (int)RegisterType == (int)SingleBitPrefix.Y) && addressStart >= 110)
                throw new NotSupportedException("XY area addresses cant be greater than 110");

            if (specialAddress > 0xF)
                throw new NotSupportedException("Special address cant be greater than 15 or 0xF");

            base.CheckAddressOverflow(addressStart, addressLen);

        }

        /// <inheritdoc/>
        public override byte? GetSpecialAddress() => SpecialAddress;

        /// <inheritdoc/>
        public override string GetMewName() {

            var spAdressEnd = SpecialAddress.ToString("X1");

            if (MemoryAddress == 0) {

                return $"{GetRegisterString()}{spAdressEnd}";

            }

            if (MemoryAddress > 0 && SpecialAddress != 0) {

                return $"{GetRegisterString()}{MemoryAddress}{spAdressEnd}";

            }

            return $"{GetRegisterString()}{MemoryAddress}";

        }

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen() => 1;

    }

}
