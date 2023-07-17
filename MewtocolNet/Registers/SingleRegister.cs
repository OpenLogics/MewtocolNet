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
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class SingleRegister<T> : Register {

        internal uint byteLength;

        internal uint addressLength;

        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public uint AddressLength => addressLength;

        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public SingleRegister() =>
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");

        internal SingleRegister(uint _address, uint _reservedByteSize, string _name = null) {

            memoryAddress = _address;
            name = _name;
            Resize(_reservedByteSize);

            if (_reservedByteSize == 2) RegisterType = RegisterType.DT;
            if(_reservedByteSize == 4) RegisterType = RegisterType.DDT;
            if (typeof(T) == typeof(string)) RegisterType = RegisterType.DT_BYTE_RANGE;

            CheckAddressOverflow(memoryAddress, addressLength);

            lastValue = null;

        }

        private void Resize (uint reservedByteSize) {

            addressLength = reservedByteSize / 2;
            if (reservedByteSize % 2 != 0) addressLength++;
            byteLength = reservedByteSize;

        }

        /// <inheritdoc/>
        public override string GetAsPLC() {

            if (typeof(T) == typeof(TimeSpan)) return ((TimeSpan)Value).ToPlcTime();

            return Value.ToString();

        }

        /// <inheritdoc/>
        public override string GetValueString() {

            if (Value != null && typeof(T) == typeof(TimeSpan)) return $"{Value} [{((TimeSpan)Value).ToPlcTime()}]";

            if (Value != null && typeof(T) == typeof(Word)) return $"{Value} [{((Word)Value).ToStringBitsPlc()}]";

            if (Value != null && typeof(T) == typeof(DWord)) return $"{Value} [{((DWord)Value).ToStringBitsPlc()}]";

            var hasFlags = typeof(T).GetCustomAttribute<FlagsAttribute>() != null;

            if (Value != null && typeof(T).IsEnum && !hasFlags) {

                var underlying = Enum.GetUnderlyingType(typeof(T));
                object val = Convert.ChangeType(Value, underlying);
                return $"{Value} [{val}]";
            } 
            
            if (Value != null && typeof(T).IsEnum && hasFlags) return $"{Value}";

            return Value?.ToString() ?? "null";

        }

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen() => AddressLength;

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object value) {

            var encoded = PlcValueParser.Encode(this, (T)value);
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

            return res;

        }

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            var res = await attachedInterface.ReadByteRangeNonBlocking((int)MemoryAddress, (int)GetRegisterAddressLen() * 2);
            if (res == null) return null;

            var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
            .FirstOrDefault(x => x.IsSameAddressAndType(this));

            if (matchingReg != null) {

                if (matchingReg is SingleRegister<string> sreg && this is SingleRegister<string> selfSreg) {
                    sreg.addressLength = selfSreg.addressLength;
                }

                matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, res);

            }

            return SetValueFromBytes(res);

        }

        internal override object SetValueFromBytes (byte[] bytes) {

            //if string correct the sizing of the byte hint was wrong
            if (typeof(T) == typeof(string)) {
                var reservedSize = BitConverter.ToInt16(bytes, 0);
                if (reservedSize != byteLength - 4)
                    throw new NotSupportedException(
                        $"The STRING register at {GetMewName()} is not correctly sized, " +
                        $"the size should be STRING[{reservedSize}] instead of STRING[{byteLength - 4}]"
                    );
            }

            AddSuccessRead();

            var parsed = PlcValueParser.Parse<T>(this, bytes);

            UpdateHoldingValue(parsed);
            return parsed;

        }

        internal override void UpdateHoldingValue(object val) {

            if (lastValue?.ToString() != val?.ToString()) {

                var beforeVal = lastValue;
                var beforeValStr = GetValueString();

                lastValue = val;

                TriggerNotifyChange();
                attachedInterface.InvokeRegisterChanged(this, beforeVal, beforeValStr);

            }

        }

    }

}
