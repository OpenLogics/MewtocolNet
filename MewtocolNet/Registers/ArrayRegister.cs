using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a string
    /// </summary>
    public class ArrayRegister<T> : Register {

        internal int[] indices;

        internal uint addressLength;

        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public uint AddressLength => addressLength;

        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public ArrayRegister() =>
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");

        internal ArrayRegister(uint _address, uint _reservedByteSize, int[] _indicies, string _name = null) {

            name = _name;
            memoryAddress = _address;
            indices = _indicies;   

            //calc mem length 
            //because one register is always 1 word (2 bytes) long, if the bytecount is uneven we get the trailing word too
            var byteSize = _reservedByteSize;
            if (byteSize % 2 != 0) byteSize++;

            RegisterType = RegisterType.DT_BYTE_RANGE;
            addressLength = Math.Max((byteSize / 2), 1);

            CheckAddressOverflow(memoryAddress, addressLength);

            lastValue = null;

        }

        public override string GetValueString() {

            if (Value == null) return "null";

            if(typeof(T) == typeof(byte[])) {

                return ((byte[])Value).ToHexString("-");

            }

            StringBuilder sb = new StringBuilder();
            var valueIenum = (IEnumerable)Value;

            foreach (var el in valueIenum) {

                sb.Append($"{el}, ");

            }
          
            return ArrayToString((Array)Value);

        }

        /// <inheritdoc/>
        public override string GetRegisterString() => "DT";

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

            //var matchingReg = attachedInterface.memoryManager.GetAllRegisters()
            //.FirstOrDefault(x => x.IsSameAddressAndType(this));

            //if (matchingReg != null)
            //    matchingReg.underlyingMemory.SetUnderlyingBytes(matchingReg, res);

            return SetValueFromBytes(res);

        }

        internal override object SetValueFromBytes(byte[] bytes) {

            AddSuccessRead();

            var parsed = PlcValueParser.ParseArray<T>(this, indices, bytes);
            UpdateHoldingValue(parsed);

            return parsed;

        }

        /// <inheritdoc/>
        internal override void UpdateHoldingValue(object val) {

            bool changeTriggerBitArr = val is BitArray bitArr &&
                                 lastValue is BitArray bitArr2 &&
                                 (bitArr.ToBitString() != bitArr2.ToBitString());

            bool changeTriggerGeneral = (lastValue?.ToString() != val?.ToString());

            if (changeTriggerBitArr || changeTriggerGeneral) {

                var beforeVal = lastValue;
                var beforeValStr = GetValueString();

                lastValue = val;

                TriggerNotifyChange();
                attachedInterface.InvokeRegisterChanged(this, beforeVal, beforeValStr);

            }

        }


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
