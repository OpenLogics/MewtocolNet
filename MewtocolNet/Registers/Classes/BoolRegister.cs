using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a boolean
    /// </summary>
    public class BoolRegister : Register, IRegister<bool> {

        internal byte specialAddress;

        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public byte SpecialAddress => specialAddress;

        public bool? Value => (bool?)ValueObj;

        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public BoolRegister() =>
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");

        internal BoolRegister(SingleBitPrefix _io, byte _spAddress = 0x0, uint _areaAdress = 0, string _name = null) : base() {

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

        /// <inheritdoc/>
        public async Task WriteAsync(bool value) {

            var res = await WriteSingleBitAsync(value);

            if (res) {

                //find the underlying memory
                var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
                .FirstOrDefault(x => x.IsSameAddressAndType(this));

                if (matchingReg != null)
                    matchingReg.underlyingMemory.SetUnderlyingBits(matchingReg, specialAddress, value);

                AddSuccessWrite();
                UpdateHoldingValue(value);

            }

        }

        private async Task<bool> WriteSingleBitAsync(bool val) {

            var rawAddr = $"{MemoryAddress}{SpecialAddress.ToString("X1")}".PadLeft(4, '0');

            string addStr = $"{GetRegisterString()}{rawAddr}";
            string cmd = $"%{attachedInterface.GetStationNumber()}#WCS{addStr}{(val ? "1" : "0")}";
            var res = await attachedInterface.SendCommandInternalAsync(cmd);

            return res.Success;

        }

        /// <inheritdoc/>
        public async Task<bool> ReadAsync() {

            var res = await attachedInterface.ReadAreaByteRangeAsync((int)MemoryAddress, (int)GetRegisterAddressLen() * 2);
            if (res == null) throw new Exception($"Failed to read the register {this}");

            var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
            .FirstOrDefault(x => x.IsSameAddressAndType(this));

            if (matchingReg != null) {

                matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, res);

            }

            return (bool)SetValueFromBytes(res);

        }

        internal override object SetValueFromBytes(byte[] bytes) {

            AddSuccessRead();

            var bitArrVal = new BitArray(bytes)[SpecialAddress];

            UpdateHoldingValue(bitArrVal);

            return bitArrVal;

        }

    }

}
