using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a string
    /// </summary>
    public class StringRegister : BaseRegister {

        internal int addressLength;
        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public int AddressLength => addressLength;

        internal short ReservedSize { get; set; }

        /// <summary>
        /// Defines a register containing a string
        /// </summary>
        public StringRegister (int _adress, int _reservedByteSize, string _name = null) {

            if (_adress > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");
            name = _name;
            memoryAddress = _adress;
            ReservedSize = (short)_reservedByteSize;

            //calc mem length
            var wordsize = (double)_reservedByteSize / 2;
            if (wordsize % 2 != 0) {
                wordsize++;
            }

            RegisterType = RegisterType.DT_BYTE_RANGE;
            addressLength = (int)Math.Round(wordsize + 1);

        }

        /// <inheritdoc/>
        public override string BuildMewtocolQuery() {

            StringBuilder asciistring = new StringBuilder("D");

            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAddress + AddressLength).ToString().PadLeft(5, '0'));

            return asciistring.ToString();
        }

        /// <inheritdoc/>
        public override void SetValueFromPLC (object val) {

            lastValue = (byte[])val;

            TriggerChangedEvnt(this);
            TriggerNotifyChange();

        }

        /// <inheritdoc/>
        public override string GetRegisterString() => "DT";

        /// <inheritdoc/>
        public override void ClearValue() => SetValueFromPLC(null);

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            var read = await attachedInterface.ReadRawRegisterAsync(this);
            return PlcValueParser.Parse<byte[]>(read);

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            return await attachedInterface.WriteRawRegisterAsync(this, (byte[])data);

        }

    }

}
