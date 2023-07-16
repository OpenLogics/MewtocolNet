using MewtocolNet.Exceptions;
using MewtocolNet.Logging;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a number
    /// </summary>
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class SingleRegister<T> : Register {

        internal uint addressLength;

        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public uint AddressLength => addressLength;


        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public SingleRegister() =>
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");

        internal SingleRegister(uint _address, uint _reservedByteSize, DynamicSizeState dynamicSizeSt, string _name = null) {

            memoryAddress = _address;
            name = _name;
            dynamicSizeState = dynamicSizeSt;
            addressLength = _reservedByteSize / 2;

            if (_reservedByteSize == 2) RegisterType = RegisterType.DT;
            if(_reservedByteSize == 4) RegisterType = RegisterType.DDT;
            if (typeof(T) == typeof(string)) RegisterType = RegisterType.DT_BYTE_RANGE;

            CheckAddressOverflow(memoryAddress, addressLength);

            lastValue = null;

        }

        /// <inheritdoc/>
        public override string GetAsPLC() {

            if (typeof(T) == typeof(TimeSpan)) return ((TimeSpan)Value).ToPlcTime();

            return Value.ToString();

        }

        /// <inheritdoc/>
        public override string GetValueString() {

            if (Value != null && typeof(T) == typeof(TimeSpan)) {

                return $"{Value} [{((TimeSpan)Value).ToPlcTime()}]";

            }

            if (Value != null && typeof(T) == typeof(Word)) {

                return $"{Value} [{((Word)Value).ToStringBitsPlc()}]";

            }

            if (Value != null && typeof(T).IsEnum) {

                var underlying = Enum.GetUnderlyingType(typeof(T));
                object val = Convert.ChangeType(Value, underlying);

                return $"{Value} [{val}]";

            }

            return Value?.ToString() ?? "null";

        }

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen() => AddressLength;

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object value) {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            if (dynamicSizeState.HasFlag(DynamicSizeState.DynamicallySized | DynamicSizeState.NeedsSizeUpdate))
                await UpdateDynamicSize();

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

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            if(dynamicSizeState.HasFlag(DynamicSizeState.DynamicallySized | DynamicSizeState.NeedsSizeUpdate))
                await UpdateDynamicSize();

            var res = await attachedInterface.ReadByteRangeNonBlocking((int)MemoryAddress, (int)GetRegisterAddressLen() * 2);
            if (res == null) return null;

            var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
            .FirstOrDefault(x => x.IsSameAddressAndType(this));

            if (matchingReg != null) {

                if (matchingReg is SingleRegister<string> sreg && this is SingleRegister<string> selfSreg) {
                    sreg.addressLength = selfSreg.addressLength;
                    sreg.dynamicSizeState = DynamicSizeState.DynamicallySized | DynamicSizeState.WasSizeUpdated;
                }

                matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, res);

            }
                

            return SetValueFromBytes(res);

        }

        internal override async Task UpdateDynamicSize() {

            if (typeof(T) == typeof(string)) await UpdateDynamicSizeString();

            dynamicSizeState = DynamicSizeState.DynamicallySized | DynamicSizeState.WasSizeUpdated;

        }

        private async Task UpdateDynamicSizeString () {

            Logger.Log($"Calibrating dynamic register ({GetRegisterWordRangeString()}) from PLC source", LogLevel.Verbose, attachedInterface);

            //get the string describer bytes
            var bytes = await attachedInterface.ReadByteRangeNonBlocking((int)MemoryAddress, 4);

            if (bytes == null || bytes.Length == 0 || bytes.All(x => x == 0x0)) {

                throw new MewtocolException($"The string register ({GetMewName()}{MemoryAddress}) doesn't exist in the PLC program");

            }

            var reservedSize = BitConverter.ToInt16(bytes, 0);
            var usedSize = BitConverter.ToInt16(bytes, 2);
            var wordsSize = Math.Max(0, (uint)(2 + (reservedSize + 1) / 2));

            addressLength = wordsSize;

            CheckAddressOverflow(memoryAddress, wordsSize);

        }

        internal override object SetValueFromBytes(byte[] bytes) {

            AddSuccessRead();

            var parsed = PlcValueParser.Parse<T>(this, bytes);
            UpdateHoldingValue(parsed);
            return parsed;

        }

        internal override void UpdateHoldingValue(object val) {

            if (lastValue?.ToString() != val?.ToString()) {

                if (val != null) lastValue = (T)val;
                else lastValue = null;

                TriggerNotifyChange();
                attachedInterface.InvokeRegisterChanged(this);

            }

        }

    }

}
