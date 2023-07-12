using MewtocolNet.Exceptions;
using System;
using System.Collections;
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

        internal uint addressLength;
        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public uint AddressLength => addressLength;

        internal uint ReservedBytesSize { get; set; }

        internal ushort? ReservedBitSize { get; set; }

        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public BytesRegister() =>
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");

        internal BytesRegister(uint _address, uint _reservedByteSize, string _name = null) {

            name = _name;
            memoryAddress = _address;
            ReservedBytesSize = _reservedByteSize;

            //calc mem length 
            //because one register is always 1 word (2 bytes) long, if the bytecount is uneven we get the trailing word too
            var byteSize = ReservedBytesSize;
            if (ReservedBytesSize % 2 != 0) byteSize++;

            RegisterType = RegisterType.DT_BYTE_RANGE;
            addressLength = Math.Max((byteSize / 2), 1);

            CheckAddressOverflow(memoryAddress, addressLength);

            lastValue = null;

        }

        public BytesRegister(uint _address, ushort _reservedBitSize, string _name = null) {

            name = _name;
            memoryAddress = _address;
            ReservedBytesSize = (uint)Math.Max(1, _reservedBitSize / 8);
            ReservedBitSize = _reservedBitSize;

            //calc mem length 
            //because one register is always 1 word (2 bytes) long, if the bytecount is uneven we get the trailing word too
            var byteSize = ReservedBytesSize;
            if (ReservedBytesSize % 2 != 0) byteSize++;

            RegisterType = RegisterType.DT_BYTE_RANGE;
            addressLength = Math.Max((byteSize / 2), 1);

            CheckAddressOverflow(memoryAddress, addressLength);

            lastValue = null;

        }

        public override string GetValueString() {

            if (Value == null) return "null";

            if(Value != null && Value is BitArray bitArr) {

                return bitArr.ToBitString();

            } else {

                return ((byte[])Value).ToHexString("-");

            }

        }

        /// <inheritdoc/>
        public override string BuildMewtocolQuery() {

            StringBuilder asciistring = new StringBuilder("D");

            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAddress + AddressLength - 1).ToString().PadLeft(5, '0'));

            return asciistring.ToString();
        }

        /// <inheritdoc/>
        public override void SetValueFromPLC (object val) {

            bool changeTriggerBitArr = val is BitArray bitArr &&
                                 lastValue is BitArray bitArr2 &&
                                 (bitArr.ToBitString() != bitArr2.ToBitString());

            bool changeTriggerGeneral = (lastValue?.ToString() != val?.ToString());

            if (changeTriggerBitArr || changeTriggerGeneral) {

                lastValue = val;

                TriggerNotifyChange();
                attachedInterface.InvokeRegisterChanged(this);

            }

        }

        /// <inheritdoc/>
        public override string GetRegisterString() => "DT";

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen() => AddressLength;

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            var res = await underlyingMemory.ReadRegisterAsync(this);
            if (!res) return null;

            var bytes = underlyingMemory.GetUnderlyingBytes(this);

            return SetValueFromBytes(bytes);

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            byte[] encoded;

            if (ReservedBitSize != null) {
                encoded = PlcValueParser.Encode(this, (BitArray)data);
            } else {
                encoded = PlcValueParser.Encode(this, (byte[])data);
            }

            var res = await underlyingMemory.WriteRegisterAsync(this, encoded);
            if (res) {
                AddSuccessWrite();
                SetValueFromPLC(data);
            }

            return res;

        }

        internal override object SetValueFromBytes(byte[] bytes) {

            AddSuccessRead();

            object parsed;
            if (ReservedBitSize != null) {
                parsed = PlcValueParser.Parse<BitArray>(this, bytes);
            } else {
                parsed = PlcValueParser.Parse<byte[]>(this, bytes);
            }

            SetValueFromPLC(parsed);
            return parsed;

        }

        internal override async Task<bool> WriteToAnonymousAsync(object value) {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            return await attachedInterface.WriteByteRange((int)MemoryAddress, (byte[])value);

        }

        internal override async Task<object> ReadFromAnonymousAsync() {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            var res = await attachedInterface.ReadByteRangeNonBlocking((int)MemoryAddress, (int)GetRegisterAddressLen() * 2, false);
            if (res == null) return null;

            return res;

        }

    }

}
