using System;

namespace MewtocolNet.Registers {
    /// <summary>
    /// Defines a register containing a number
    /// </summary>
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class NRegister<T> : Register {

        public T NeedValue;
        public T LastValue;

        /// <summary>
        /// The value of the register
        /// </summary>
        public T Value => LastValue;

        /// <summary>
        /// Defines a register containing a number
        /// </summary>
        /// <param name="_adress">Memory start adress max 99999</param>
        /// <param name="_name">Name of the register</param>
        public NRegister (int _adress, string _name = null) {

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

        internal void SetValueFromPLC (object val) {
            LastValue = (T)val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();
        }

        public override string ToString() {
            return $"Adress: {MemoryAdress} Val: {Value}";
        }

    }




}
