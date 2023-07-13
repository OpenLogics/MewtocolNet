using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace MewtocolNet {

    /// <summary>
    /// A DWord is a 16 bit value of 2 bytes
    /// </summary>
    public struct DWord : MewtocolExtensionTypeDDT {

        private int bitLength;

        internal uint value;

        public uint Value {
            get => value;
            set {
                this.value = value;
            }
        }

        public DWord(uint bytes) {
            value = bytes;
            bitLength = Marshal.SizeOf(value) * 8;
        }
        public DWord(byte[] bytes) {
            bytes = bytes.Take(4).ToArray();
            value = BitConverter.ToUInt32(bytes, 0);
            bitLength = Marshal.SizeOf(value) * 8;
        }

        //operations

        public static DWord operator -(DWord a, DWord b) => new DWord() {
            value = (ushort)(a.value - b.value)
        };

        public static DWord operator +(DWord a, DWord b) => new DWord() {
            value = (ushort)(a.value + b.value)
        };

        public static DWord operator *(DWord a, DWord b) => new DWord() {
            value = (ushort)(a.value * b.value)
        };

        public static DWord operator /(DWord a, DWord b) => new DWord() {
            value = (ushort)(a.value / b.value)
        };

        public static bool operator ==(DWord a, DWord b) => a.value == b.value;

        public static bool operator !=(DWord a, DWord b) => a.value != b.value;

        /// <summary>
        /// Gets or sets the bit value at the given position
        /// </summary>
        public bool this[int bitIndex] {
            get {
                if (bitIndex > bitLength - 1)
                    throw new IndexOutOfRangeException($"The DWord bit index was out of range ({bitIndex}/{bitLength - 1})");
                
                return (value & (1 << bitIndex)) != 0;
            }
            set {
                if (bitIndex > bitLength - 1)
                    throw new IndexOutOfRangeException($"The DWord bit index was out of range ({bitIndex}/{bitLength - 1})");

                int mask = 1 << bitIndex;
                this.value = value ? this.value |= (uint)mask : this.value &= (uint)~mask;
            }
        }

        public void ClearBits () => this.value = 0;

        public override bool Equals(object obj) {

            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            } else {
                return (DWord)obj == this;
            }

        }

        public override int GetHashCode() => (int)value;

        public byte[] ToByteArray() => BitConverter.GetBytes(value);

        //string ops

        public override string ToString() => $"0x{value.ToString("X8")}";

        public string ToStringBits () {

            return Convert.ToString(value, 2).PadLeft(bitLength, '0');
        
        }

        public string ToStringBitsPlc () {

            var parts = Convert.ToString(value, 2)
            .PadLeft(Marshal.SizeOf(value) * 8, '0')
            .SplitInParts(4);

            return string.Join("_", parts);

        }

    }

}
