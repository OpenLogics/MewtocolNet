using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a string
    /// </summary>
    public class BytesRegister : BaseRegister {

        internal int addressLength;
        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public int AddressLength => addressLength;

        internal short ReservedSize { get; set; }

        /// <summary>
        /// Defines a register containing a string
        /// </summary>
        public BytesRegister(int _address, int _reservedByteSize, string _name = null) {

            if (_address > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");
            name = _name;
            memoryAddress = _address;
            ReservedSize = (short)_reservedByteSize;

            //calc mem length 
            //because one register is always 1 word (2 bytes) long, if the bytecount is uneven we get the trailing word too
            var byteSize = _reservedByteSize;
            if (_reservedByteSize % 2 != 0) byteSize++;

            RegisterType = RegisterType.DT_BYTE_RANGE;
            addressLength = (byteSize / 2) - 1;

        }

        public override string GetValueString() => Value == null ? "null" : ((byte[])Value).ToHexString("-");

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

            if (!attachedInterface.IsConnected) return null;

            var read = await attachedInterface.ReadRawRegisterAsync(this);
            if (read == null) return null;

            var parsed = PlcValueParser.Parse<byte[]>(this, read);

            SetValueFromPLC(parsed);
            return parsed;

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            if (!attachedInterface.IsConnected) return false;

            return await attachedInterface.WriteRawRegisterAsync(this, (byte[])data);

        }

    }

}
