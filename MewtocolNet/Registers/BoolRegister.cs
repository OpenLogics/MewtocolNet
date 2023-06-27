using System;
using System.ComponentModel;
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
        public BoolRegister(IOType _io, byte _spAddress = 0x0, int _areaAdress = 0, string _name = null) {

            if (_areaAdress < 0)
                throw new NotSupportedException("The area address cant be negative");

            if (_io == IOType.R && _areaAdress >= 512)
                throw new NotSupportedException("R area addresses cant be greater than 511");

            if ((_io == IOType.X || _io == IOType.Y) && _areaAdress >= 110)
                throw new NotSupportedException("XY area addresses cant be greater than 110");

            if (_spAddress > 0xF)
                throw new NotSupportedException("Special address cant be greater 15 or 0xF");

            lastValue = false;

            memoryAddress = _areaAdress;
            specialAddress = _spAddress;
            name = _name;

            RegisterType = (RegisterType)(int)_io;

        }

        #region Read / Write

        public override void SetValueFromPLC(object val) {

            lastValue = (bool)val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();

        }

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            var read = await attachedInterface.ReadRawRegisterAsync(this);
            var parsed = PlcValueParser.Parse<bool>(read);

            SetValueFromPLC(parsed);
            return parsed;

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            return await attachedInterface.WriteRawRegisterAsync(this, PlcValueParser.Encode((bool)data));

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
        public override void ClearValue() => SetValueFromPLC(false);

        /// <inheritdoc/>
        public override string GetRegisterPLCName() {

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
        public override string ToString(bool additional) {

            if (!additional) return this.ToString();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"PLC Naming: {GetRegisterPLCName()}");
            sb.AppendLine($"Name: {Name ?? "Not named"}");
            sb.AppendLine($"Value: {GetValueString()}");
            sb.AppendLine($"Register Type: {RegisterType}");
            sb.AppendLine($"Memory Address: {MemoryAddress}");
            sb.AppendLine($"Special Address: {SpecialAddress:X1}");

            return sb.ToString();

        }

    }

}
