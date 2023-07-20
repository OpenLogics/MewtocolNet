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
    public class StructRegister<T> : Register, IRegister<T> where T : struct {

        internal uint addressLength;

        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public uint AddressLength => addressLength;

        public T? Value => (T?)ValueObj;

        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public StructRegister() =>
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");

        internal StructRegister(uint _address, uint _reservedByteSize, string _name = null) {

            memoryAddress = _address;
            name = _name;

            addressLength = _reservedByteSize / 2;
            if (_reservedByteSize % 2 != 0) addressLength++;

            if (_reservedByteSize == 2) RegisterType = RegisterType.DT;
            if(_reservedByteSize == 4) RegisterType = RegisterType.DDT;

            CheckAddressOverflow(memoryAddress, addressLength);

            underlyingSystemType = typeof(T);   
            lastValue = null;

        }

        /// <inheritdoc/>
        public override string GetAsPLC() {

            if (typeof(T) == typeof(TimeSpan)) return ((TimeSpan)(object)ValueObj).ToPlcTime();

            return ValueObj.ToString();

        }

        /// <inheritdoc/>
        public override string GetValueString() {

            if (ValueObj != null && typeof(T) == typeof(TimeSpan)) return $"{ValueObj} [{((TimeSpan)(object)ValueObj).ToPlcTime()}]";

            if (ValueObj != null && typeof(T) == typeof(Word)) return $"{ValueObj} [{((Word)(object)ValueObj).ToStringBitsPlc()}]";

            if (ValueObj != null && typeof(T) == typeof(DWord)) return $"{ValueObj} [{((DWord)(object)ValueObj).ToStringBitsPlc()}]";

            var hasFlags = typeof(T).GetCustomAttribute<FlagsAttribute>() != null;

            if (ValueObj != null && typeof(T).IsEnum && !hasFlags) {

                var underlying = Enum.GetUnderlyingType(typeof(T));
                object val = Convert.ChangeType(ValueObj, underlying);
                return $"{ValueObj} [{val}]";
            } 
            
            if (ValueObj != null && typeof(T).IsEnum && hasFlags) return $"{ValueObj}";

            return ValueObj?.ToString() ?? "null";

        }

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen() => AddressLength;

        /// <inheritdoc/>
        public async Task WriteAsync(T value) {

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

        }

        /// <inheritdoc/>
        public async Task<T> ReadAsync() {

            var res = await attachedInterface.ReadByteRangeNonBlocking((int)MemoryAddress, (int)GetRegisterAddressLen() * 2);
            if (res == null) throw new Exception($"Failed to read the register {this}");

            var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
            .FirstOrDefault(x => x.IsSameAddressAndType(this));

            if (matchingReg != null) {

                matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, res);

            }

            return (T)SetValueFromBytes(res);

        }

        internal override object SetValueFromBytes (byte[] bytes) {

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
