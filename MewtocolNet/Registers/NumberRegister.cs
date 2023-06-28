using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a number
    /// </summary>
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class NumberRegister<T> : BaseRegister {

        internal Type enumType { get; set; }

        /// <summary>
        /// Defines a register containing a number
        /// </summary>
        /// <param name="_address">Memory start adress max 99999</param>
        /// <param name="_name">Name of the register</param>
        public NumberRegister (int _address, string _name = null) {

            if (_address > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");

            memoryAddress = _address;
            name = _name;

            Type numType = typeof(T);

            var allowedTypes = PlcValueParser.GetAllowDotnetTypes();
            if (!allowedTypes.Contains(numType))
                throw new NotSupportedException($"The type {numType} is not allowed for Number Registers");

            var areaLen = (Marshal.SizeOf(numType) / 2) - 1;
            RegisterType = areaLen >= 1 ? RegisterType.DDT : RegisterType.DT;

            lastValue = default(T);

        }

        /// <summary>
        /// Defines a register containing a number
        /// </summary>
        /// <param name="_address">Memory start adress max 99999</param>
        /// <param name="_enumType">Enum type to parse as</param>
        /// <param name="_name">Name of the register</param>
        public NumberRegister(int _address, Type _enumType, string _name = null) {

            if (_address > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");

            memoryAddress = _address;
            name = _name;

            Type numType = typeof(T);

            var allowedTypes = PlcValueParser.GetAllowDotnetTypes();
            if (!allowedTypes.Contains(numType))
                throw new NotSupportedException($"The type {numType} is not allowed for Number Registers");

            var areaLen = (Marshal.SizeOf(numType) / 2) - 1;
            RegisterType = areaLen >= 1 ? RegisterType.DDT : RegisterType.DT;

            enumType = _enumType;
            lastValue = default(T);

        }

        /// <inheritdoc/>
        public override void SetValueFromPLC(object val) {

            lastValue = (T)val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();

        }

        /// <inheritdoc/>
        public override string BuildMewtocolQuery() {

            StringBuilder asciistring = new StringBuilder("D");
            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));

            int offsetAddress = 0;
            if(RegisterType == RegisterType.DDT)
                offsetAddress = 1;

            asciistring.Append((MemoryAddress + offsetAddress).ToString().PadLeft(5, '0'));
            return asciistring.ToString();

        }

        /// <inheritdoc/>
        public override string GetAsPLC() => ((TimeSpan)Value).AsPLCTime();

        /// <inheritdoc/>
        public override string GetValueString() {

            if(typeof(T) == typeof(TimeSpan)) {

                return $"{Value} [{((TimeSpan)Value).AsPLCTime()}]";

            } 

            //is number or bitwise
            if (enumType == null) {

                return $"{Value}";

            }

            //is enum
            var dict = new Dictionary<int, string>();

            foreach (var name in Enum.GetNames(enumType)) {

                int enumKey = (int)Enum.Parse(enumType, name);
                if (!dict.ContainsKey(enumKey)) {
                    dict.Add(enumKey, name);
                }

            }

            if (enumType != null && Value is short shortVal) {

                if (dict.ContainsKey(shortVal)) {

                    return $"{Value} ({dict[shortVal]})";

                } else {

                    return $"{Value} (Missing Enum)";

                }

            }

            if (enumType != null && Value is int intVal) {

                if (dict.ContainsKey(intVal)) {

                    return $"{Value} ({dict[intVal]})";

                } else {

                    return $"{Value} (Missing Enum)";

                }

            }

            return Value.ToString();

        }

        /// <inheritdoc/>
        public override void ClearValue() => SetValueFromPLC(default(T));

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            if (!attachedInterface.IsConnected) return null;

            var read = await attachedInterface.ReadRawRegisterAsync(this);
            if (read == null) return null;

            var parsed = PlcValueParser.Parse<T>(this, read);

            SetValueFromPLC(parsed);
            return parsed;

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            if (!attachedInterface.IsConnected) return false;

            return await attachedInterface.WriteRawRegisterAsync(this, PlcValueParser.Encode(this, (T)data));

        }

        /// <summary>
        /// Gets the register bitwise if its a 16 or 32 bit int
        /// </summary>
        /// <returns>A bitarray</returns>
        public BitArray GetBitwise() {

            if (this is NumberRegister<short> shortReg) {

                var bytes = BitConverter.GetBytes((short)Value);
                BitArray bitAr = new BitArray(bytes);
                return bitAr;

            }

            if (this is NumberRegister<int> intReg) {

                var bytes = BitConverter.GetBytes((int)Value);
                BitArray bitAr = new BitArray(bytes);
                return bitAr;

            }

            return null;

        }

    }

}
