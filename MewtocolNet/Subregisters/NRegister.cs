using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MewtocolNet.Subregisters {

    /// <summary>
    /// Defines a register containing a number
    /// </summary>
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class NRegister<T> : IRegister {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event Action<object> ValueChanged;

        /// <summary>
        /// Triggers when a property on the register changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        internal Type collectionType;

        /// <summary>
        /// The type of collection the register is in or null of added manually
        /// </summary>
        public Type CollectionType => collectionType;

        internal T lastValue;

        /// <summary>
        /// The value of the register
        /// </summary>
        public object Value => lastValue;

        internal string name;
        /// <summary>
        /// The register name or null of not defined
        /// </summary>
        public string Name => name;

        internal int memoryAdress;
        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public int MemoryAddress => memoryAdress;

        internal int memoryLength;
        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public int MemoryLength => memoryLength;

        internal bool isUsedBitwise { get; set; }

        internal Type enumType { get; set; }

        /// <summary>
        /// Defines a register containing a number
        /// </summary>
        /// <param name="_adress">Memory start adress max 99999</param>
        /// <param name="_name">Name of the register</param>
        public NRegister(int _adress, string _name = null) {

            if (_adress > 99999)
                throw new NotSupportedException("Memory adresses cant be greater than 99999");

            memoryAdress = _adress;
            name = _name;
            Type numType = typeof(T);
            if (numType == typeof(short)) {
                memoryLength = 0;
            } else if (numType == typeof(ushort)) {
                memoryLength = 0;
            } else if (numType == typeof(int)) {
                memoryLength = 1;
            } else if (numType == typeof(uint)) {
                memoryLength = 1;
            } else if (numType == typeof(float)) {
                memoryLength = 1;
            } else if (numType == typeof(TimeSpan)) {
                memoryLength = 1;
            } else {
                throw new NotSupportedException($"The type {numType} is not allowed for Number Registers");
            }

        }

        internal NRegister(int _adress, string _name = null, bool isBitwise = false, Type _enumType = null) {

            if (_adress > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");
            memoryAdress = _adress;
            name = _name;
            Type numType = typeof(T);
            if (numType == typeof(short)) {
                memoryLength = 0;
            } else if (numType == typeof(ushort)) {
                memoryLength = 0;
            } else if (numType == typeof(int)) {
                memoryLength = 1;
            } else if (numType == typeof(uint)) {
                memoryLength = 1;
            } else if (numType == typeof(float)) {
                memoryLength = 1;
            } else if (numType == typeof(TimeSpan)) {
                memoryLength = 1;
            } else {
                throw new NotSupportedException($"The type {numType} is not allowed for Number Registers");
            }

            isUsedBitwise = isBitwise;
            enumType = _enumType;

        }

        internal NRegister<T> WithCollectionType(Type colType) {

            collectionType = colType;
            return this;

        }

        internal void SetValueFromPLC(object val) {

            lastValue = (T)val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();

        }

        public string GetStartingMemoryArea() => MemoryAddress.ToString();

        public Type GetCollectionType() => CollectionType;

        public bool IsUsedBitwise() => isUsedBitwise;

        public string GetValueString() {

            //is number or bitwise
            if (enumType == null) {

                return $"{Value}{(isUsedBitwise ? $" [{GetBitwise().ToBitString()}]" : "")}";

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

        /// <summary>
        /// Gets the register bitwise if its a 16 or 32 bit int
        /// </summary>
        /// <returns>A bitarray</returns>
        public BitArray GetBitwise() {

            if (this is NRegister<short> shortReg) {

                var bytes = BitConverter.GetBytes((short)Value);
                BitArray bitAr = new BitArray(bytes);
                return bitAr;

            }

            if (this is NRegister<int> intReg) {

                var bytes = BitConverter.GetBytes((int)Value);
                BitArray bitAr = new BitArray(bytes);
                return bitAr;

            }

            return null;

        }

        public string BuildMewtocolQuery() {

            StringBuilder asciistring = new StringBuilder("D");
            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAddress + MemoryLength).ToString().PadLeft(5, '0'));
            return asciistring.ToString();

        }

        public string GetRegisterString() {

            if (Value is short) {
                return "DT";
            }

            if (Value is ushort) {
                return "DT";
            }

            if (Value is int) {
                return "DDT";
            }

            if (Value is uint) {
                return "DDT";
            }

            if (Value is float) {
                return "DDT";
            }

            if (Value is TimeSpan) {
                return "DDT";
            }

            throw new NotSupportedException("Numeric type is not supported");

        }

        public string GetCombinedName() => $"{(CollectionType != null ? $"{CollectionType.Name}." : "")}{Name ?? "Unnamed"}";

        public string GetContainerName() => $"{(CollectionType != null ? $"{CollectionType.Name}" : "")}";

        public string GetRegisterPLCName() => $"{GetRegisterString()}{MemoryAddress}";

        public void ClearValue() => SetValueFromPLC(default(T));

        internal void TriggerChangedEvnt(object changed) => ValueChanged?.Invoke(changed);

        public void TriggerNotifyChange() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));

        public override string ToString() => $"{GetRegisterPLCName()} - Value: {GetValueString()}";

    }

}
