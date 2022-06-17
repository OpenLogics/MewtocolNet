using System;

namespace MewtocolNet.Responses {
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
        public T Value {
            get => LastValue;
            set {
                NeedValue = value;
                TriggerChangedEvnt(this);
            }
        }

        /// <summary>
        /// Defines a register containing a number
        /// </summary>
        /// <param name="_adress">Memory start adress max 99999</param>
        /// <param name="_format">The format in which the variable is stored</param>
        public NRegister(int _adress, string _name = null, bool isBitwise = false) {

            if (_adress > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");
            MemoryAdress = _adress;
            Name = _name;
            Type numType = typeof(T);
            if (numType == typeof(short)) {
                MemoryLength = 0;
            } else if (numType == typeof(ushort)) {
                MemoryLength = 0;
            } else if (numType == typeof(int)) {
                MemoryLength = 1;
            } else if (numType == typeof(uint)) {
                MemoryLength = 1;
            } else if (numType == typeof(float)) {
                MemoryLength = 1;
            } else {
                throw new NotSupportedException($"The type {numType} is not allowed for Number Registers");
            }

            isUsedBitwise = isBitwise;

        }

        public override string ToString() {
            return $"Adress: {MemoryAdress} Val: {Value}";
        }
    }




}
