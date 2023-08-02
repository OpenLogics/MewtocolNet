using MewtocolNet.Logging;
using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a number
    /// </summary>
    public class StringRegister : Register, IStringRegister {

        internal int reservedStringLength;
        internal uint byteLength;

        internal uint addressLength;

        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public uint AddressLength => addressLength;

        public string Value => (string)ValueObj;

        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public StringRegister() =>
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");

        internal StringRegister(uint _address, uint _reservedByteSize, string _name = null) : base() {

            memoryAddress = _address;
            name = _name;

            reservedStringLength = (int)_reservedByteSize;   
            Resize(_reservedByteSize);

            RegisterType = RegisterPrefix.DT;

            CheckAddressOverflow(memoryAddress, addressLength);

            lastValue = null;

        }

        private void Resize (uint reservedByteSize) {

            if (reservedByteSize % 2 != 0) reservedByteSize++;
            reservedByteSize += 4;

            addressLength = reservedByteSize / 2;
            byteLength = reservedByteSize;

        }

        /// <inheritdoc/>
        public override string GetAsPLC() => Value;

        /// <inheritdoc/>
        public override string GetValueString() => ValueObj?.ToString() ?? "null";

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen() => AddressLength;

        /// <inheritdoc/>
        public async Task WriteAsync(string value) {

            //trim the size if the input was larger
            if(value.Length > reservedStringLength) {
                value = value.Substring(0, reservedStringLength);
            }

            var encoded = PlcValueParser.Encode(this, value);
            var res = await attachedInterface.WriteByteRange((int)MemoryAddress, encoded);

            if (res) {

                //find the underlying memory
                var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
                .FirstOrDefault(x => x.IsSameAddressAndType(this));

                if (matchingReg != null)
                    matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, encoded);

                AddSuccessWrite();
                UpdateHoldingValue(value);

            }

        }

        /// <inheritdoc/>
        public async Task<string> ReadAsync() {

            var res = await attachedInterface.ReadByteRangeNonBlocking((int)MemoryAddress, (int)GetRegisterAddressLen() * 2);
            if (res == null) return null;

            var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
            .FirstOrDefault(x => x.IsSameAddressAndType(this));

            if (matchingReg != null) {

                if (matchingReg is StringRegister sreg && this is StringRegister selfSreg) {

                    sreg.addressLength = selfSreg.addressLength;

                }

                matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, res);

            }

            return (string)SetValueFromBytes(res);

        }

        internal override object SetValueFromBytes (byte[] bytes) {

            //if string correct the sizing of the byte hint was wrong
            var reservedSize = BitConverter.ToInt16(bytes, 0);

            if (reservedStringLength != reservedSize)
                throw new NotSupportedException(
                    $"The STRING register at {GetMewName()} is not correctly sized, " +
                    $"the size should be STRING[{reservedSize}] instead of STRING[{reservedStringLength}]"
                );

            AddSuccessRead();

            var parsed = PlcValueParser.Parse<string>(this, bytes);

            UpdateHoldingValue(parsed);
            return parsed;

        }

        internal override void UpdateHoldingValue(object val) {

            TriggerUpdateReceived();

            if (lastValue?.ToString() != val?.ToString()) {

                var beforeVal = lastValue;
                var beforeValStr = GetValueString();

                lastValue = val;

                TriggerValueChange();
                attachedInterface.InvokeRegisterChanged(this, beforeVal, beforeValStr);

            }

        }

    }

}
