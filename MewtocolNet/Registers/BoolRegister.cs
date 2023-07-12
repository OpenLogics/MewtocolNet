using MewtocolNet.UnderlyingRegisters;
using System;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a boolean
    /// </summary>
    public class BoolRegister : BaseRegister {

        internal byte specialAddress;
        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public byte SpecialAddress => specialAddress;

        /// <summary>
        /// Creates a new boolean register
        /// </summary>
        /// <param name="_io">The io type prefix</param>
        /// <param name="_spAddress">The special address</param>
        /// <param name="_areaAdress">The area special address</param>
        /// <param name="_name">The custom name</param>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="Exception"></exception>
        public BoolRegister(IOType _io, byte _spAddress = 0x0, uint _areaAdress = 0, string _name = null) {

            lastValue = null;

            memoryAddress = _areaAdress;
            specialAddress = _spAddress;
            name = _name;

            RegisterType = (RegisterType)(int)_io;

            CheckAddressOverflow(memoryAddress, 0);

        }

        protected override void CheckAddressOverflow(uint addressStart, uint addressLen) {

            if ((int)RegisterType == (int)IOType.R && addressStart >= 512)
                throw new NotSupportedException("R area addresses cant be greater than 511");

            if (((int)RegisterType == (int)IOType.X || (int)RegisterType == (int)IOType.Y) && addressStart >= 110)
                throw new NotSupportedException("XY area addresses cant be greater than 110");

            if (specialAddress > 0xF)
                throw new NotSupportedException("Special address cant be greater 15 or 0xF");

            base.CheckAddressOverflow(addressStart, addressLen);

        }

        #region Read / Write

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            if (!attachedInterface.IsConnected) return null;

            var read = await attachedInterface.ReadRawRegisterAsync(this);
            if(read == null) return null;

            var parsed = PlcValueParser.Parse<bool>(this, read);

            SetValueFromPLC(parsed);
            return parsed;

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            if (!attachedInterface.IsConnected) return false;

            var encoded = PlcValueParser.Encode(this, (bool)data);

            var res = await attachedInterface.WriteRawRegisterAsync(this, encoded);
            if (res) {
                SetValueFromPLC(data);
            }

            return res;

        }

        #endregion

        /// <inheritdoc/>
        public override byte? GetSpecialAddress() => SpecialAddress;

        /// <inheritdoc/>
        public override string BuildMewtocolQuery() {

            //(R|X|Y)(area add [3] + special add [1])
            StringBuilder asciistring = new StringBuilder();

            string prefix = RegisterType.ToString();
            string mem = MemoryAddress.ToString();
            string sp = SpecialAddress.ToString("X1");

            asciistring.Append(prefix);
            asciistring.Append(mem.PadLeft(3, '0'));
            asciistring.Append(sp);

            return asciistring.ToString();

        }

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
        public override uint GetRegisterAddressLen () => 1;

    }

}
