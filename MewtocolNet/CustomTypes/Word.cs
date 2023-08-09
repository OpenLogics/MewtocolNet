using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MewtocolNet {

    /// <summary>
    /// A word is a 16 bit value of 2 bytes
    /// </summary>
    public struct Word : MewtocolExtTypeInit1Word {

        private int bitLength;

        internal ushort value;

        public ushort Value => value;

        public Word(ushort bytes) {
            value = bytes;
            bitLength = Marshal.SizeOf(value) * 8;
        }

        public Word(byte[] bytes) {
            bytes = bytes.Take(2).ToArray();
            value = BitConverter.ToUInt16(bytes, 0);
            bitLength = Marshal.SizeOf(value) * 8;
        }

        //operations

        public static Word operator -(Word a, Word b) => new Word() {
            value = (ushort)(a.value - b.value)
        };

        public static Word operator +(Word a, Word b) => new Word() {
            value = (ushort)(a.value + b.value)
        };

        public static Word operator *(Word a, Word b) => new Word() {
            value = (ushort)(a.value * b.value)
        };

        public static Word operator /(Word a, Word b) => new Word() {
            value = (ushort)(a.value / b.value)
        };

        public static bool operator ==(Word a, Word b) => a.value == b.value;

        public static bool operator !=(Word a, Word b) => a.value != b.value;

        /// <summary>
        /// Gets the bit value at the given position
        /// </summary>
        public bool this[int bitIndex] {
            get {

                if (bitIndex > bitLength - 1 && bitLength != 0)
                    throw new IndexOutOfRangeException($"The word bit index was out of range ({bitIndex}/{bitLength - 1})");

                if (bitLength == 0) return false;

                return (value & (1 << bitIndex)) != 0;
            
            }
        }

        public override bool Equals(object obj) {

            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            } else {
                return (Word)obj == this;
            }

        }

        public override int GetHashCode() => (int)value;

        public byte[] ToByteArray() => BitConverter.GetBytes(value);

        //string ops

        public override string ToString() => $"0x{value.ToString("X4")}";

        public string ToStringBits() {

            return Convert.ToString(value, 2).PadLeft(bitLength, '0');

        }

        public string ToStringBitsPlc() {

            var parts = Convert.ToString(value, 2)
            .PadLeft(Marshal.SizeOf(value) * 8, '0')
            .SplitInParts(4);

            return string.Join("_", parts);

        }

    }

}
