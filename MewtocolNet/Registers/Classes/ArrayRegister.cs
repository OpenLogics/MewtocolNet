using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a string
    /// </summary>
    public class ArrayRegister<T> : Register, IArrayRegister<T>, IArrayRegister2D<T>, IArrayRegister3D<T> {

        internal int byteSizePerItem;
        internal uint reservedByteSize;

        internal int[] indices;

        internal uint addressLength;

        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public uint AddressLength => addressLength;

        T[] IArrayRegister<T>.Value => (T[])ValueObj;
        
        T[,] IArrayRegister2D<T>.Value => (T[,])ValueObj;

        T[,,] IArrayRegister3D<T>.Value => (T[,,])ValueObj;

        public int Count => ((Array)ValueObj).Length;

        public T this[int i1, int i2, int i3] => (T)((Array)ValueObj).GetValue(i1, i2, i3);

        public T this[int i1, int i2] => (T)((Array)ValueObj).GetValue(i1, i2);

        public T this[int i] => (T)((Array)ValueObj).GetValue(i);

        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public ArrayRegister() =>
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");

        internal ArrayRegister(uint _address, uint _reservedByteSize, int[] _indices, string _name = null) : base() {

            if (_reservedByteSize % 2 != 0)
                throw new ArgumentException(nameof(_reservedByteSize), "Reserved byte size must be even");

            name = _name;
            memoryAddress = _address;
            indices = _indices;

            int itemCount = _indices.Aggregate((a, x) => a * x);
            byteSizePerItem = (int)_reservedByteSize / itemCount;
            reservedByteSize = _reservedByteSize;

            RegisterType = byteSizePerItem == 4 ? RegisterPrefix.DDT : RegisterPrefix.DT;
            addressLength = Math.Max((_reservedByteSize / 2), 1);

            CheckAddressOverflow(memoryAddress, addressLength);

            underlyingSystemType = typeof(T).MakeArrayType(indices.Length);

            lastValue = null;
        }

        public IEnumerator<T> GetEnumerator() => ((Array)ValueObj)?.OfType<T>()?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((Array)ValueObj)?.OfType<T>()?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();    

        async Task<T[]> IArrayRegister<T>.ReadAsync() => (T[])(object)await ReadAsync();

        async Task<T[,]> IArrayRegister2D<T>.ReadAsync() => (T[,])(object)await ReadAsync();

        async Task<T[,,]> IArrayRegister3D<T>.ReadAsync() => (T[,,])(object)await ReadAsync();

        async Task IArrayRegister<T>.WriteAsync(T[] data) => await WriteAsync(data);

        async Task IArrayRegister2D<T>.WriteAsync(T[,] data) => await WriteAsync(data);

        async Task IArrayRegister3D<T>.WriteAsync(T[,,] data) => await WriteAsync(data);

        /// <inheritdoc/>
        private async Task WriteAsync(object value) {

            var encoded = PlcValueParser.EncodeArray(this, value);
            
            var res = await attachedInterface.WriteByteRange((int)MemoryAddress, encoded);

            if (res) {

                if(isAnonymous) {

                    //find the underlying memory
                    var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
                    .FirstOrDefault(x => x.IsSameAddressAndType(this));

                    if (matchingReg != null) matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, encoded);

                } else {

                    underlyingMemory.SetUnderlyingBytes(this, encoded);

                }

                AddSuccessWrite();
                UpdateHoldingValue(value);

            }

        }

        private async Task WriteEntriesAsync(int start, object value) {

            var encoded = PlcValueParser.EncodeArray(this, value);

            var res = await attachedInterface.WriteByteRange((int)MemoryAddress, encoded);

            if (res) {

                if (isAnonymous) {

                    //find the underlying memory
                    var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
                    .FirstOrDefault(x => x.IsSameAddressAndType(this));

                    if (matchingReg != null) matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, encoded);

                } else {

                    underlyingMemory.SetUnderlyingBytes(this, encoded);

                }

                AddSuccessWrite();
                UpdateHoldingValue(value);

            }

        }

        /// <inheritdoc/>
        private async Task<object> ReadAsync() {

            var res = await attachedInterface.ReadByteRangeNonBlocking((int)MemoryAddress, (int)GetRegisterAddressLen() * 2);
            if (res == null) throw new Exception();

            var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
            .FirstOrDefault(x => x.IsSameAddressAndType(this));

            if (matchingReg != null)
                matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, res);

            return SetValueFromBytes(res);

        }

        internal override object SetValueFromBytes(byte[] bytes) {

            if (bytes.Length != reservedByteSize)
                throw new ArgumentException(nameof(bytes), "Bytes were not equal the size of registers reserved byte size");

            AddSuccessRead();

            var parsed = PlcValueParser.ParseArray(this, bytes);
            UpdateHoldingValue(parsed);

            return parsed;

        }

        /// <inheritdoc/>
        internal override void UpdateHoldingValue(object val) {

            TriggerUpdateReceived();

            if (val == null && lastValue == null) return;

            bool sequenceDifference = false;

            if(val == null && lastValue != null) sequenceDifference = true;
            if(val != null && lastValue == null) sequenceDifference = true;
            if (val != null && lastValue != null) {

                var val1 = ((Array)val).OfType<T>();
                var val2 = ((Array)lastValue).OfType<T>();

                sequenceDifference = !Enumerable.SequenceEqual(val1, val2);
            
            }

            if (sequenceDifference) {

                var beforeVal = lastValue;
                var beforeValStr = GetValueString();

                lastValue = val;

                TriggerValueChange();
                attachedInterface.InvokeRegisterChanged(this, beforeVal, beforeValStr);

            }

        }

        public override string GetValueString() {

            if (ValueObj == null) return "null";

            if (underlyingSystemType == typeof(byte[])) {

                return ((byte[])ValueObj).ToHexString("-");

            }

            return ArrayToString((Array)ValueObj);

        }

        /// <inheritdoc/>
        public override string GetRegisterString() => "DT";

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen() => AddressLength;

        private string ArrayToString(Array array) {

            int rank = array.Rank;
            int[] lengths = new int[rank];
            int[] indices = new int[rank];
            for (int i = 0; i < rank; i++) {
                lengths[i] = array.GetLength(i);
            }

            string result = "[";
            result += ArrayToStringRecursive(array, lengths, indices, 0);
            result += "]";
            return result;

        }

        private string ArrayToStringRecursive(Array array, int[] lengths, int[] indices, int dimension) {

            if (dimension == array.Rank - 1) {
                
                string result = "[";
                for (indices[dimension] = 0; indices[dimension] < lengths[dimension]; indices[dimension]++) {
                    result += array.GetValue(indices).ToString();
                    if (indices[dimension] < lengths[dimension] - 1) {
                        result += ",";
                    }
                }
                result += "]";
                return result;

            } else {
                string result = "[";
                for (indices[dimension] = 0; indices[dimension] < lengths[dimension]; indices[dimension]++) {
                    result += ArrayToStringRecursive(array, lengths, indices, dimension + 1);
                    if (indices[dimension] < lengths[dimension] - 1) {
                        result += ",";
                    }
                }
                result += "]";
                return result;
            }

        }


    }

}
