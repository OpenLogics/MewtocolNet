using MewtocolNet.Exceptions;
using MewtocolNet.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a string
    /// </summary>
    public class StringRegister : BaseRegister {

        internal short ReservedSize { get; set; }

        internal short UsedSize { get; set; }   

        internal uint WordsSize { get; set; }  

        private bool isCalibratedFromPlc = false;

        /// <summary>
        /// Defines a register containing a string
        /// </summary>
        public StringRegister (uint _address, string _name = null) {

            name = _name;
            memoryAddress = _address;
            RegisterType = RegisterType.DT_BYTE_RANGE;

            CheckAddressOverflow(memoryAddress, 0);

            lastValue = null;

        }

        /// <inheritdoc/>
        public override string BuildMewtocolQuery() {

            StringBuilder asciistring = new StringBuilder("D");

            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAddress + Math.Max(1, WordsSize) - 1).ToString().PadLeft(5, '0'));

            return asciistring.ToString();
        }

        /// <inheritdoc/>
        public override string GetValueString() => $"'{Value}'";

        /// <inheritdoc/>
        public override void SetValueFromPLC (object val) {

            if (!val.Equals(lastValue)) {

                lastValue = (string)val;

                TriggerNotifyChange();
                attachedInterface.InvokeRegisterChanged(this);

            }

        }

        /// <inheritdoc/>
        public override string GetRegisterString() => "DT";

        /// <inheritdoc/>
        public override void ClearValue() => SetValueFromPLC("");

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen() => Math.Max(1, WordsSize);

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            if (!attachedInterface.IsConnected) return null;

            //get the string params first

            if(!isCalibratedFromPlc) await CalibrateFromPLC();

            var read = await attachedInterface.ReadRawRegisterAsync(this);
            if (read == null) return null;

            var parsed = PlcValueParser.Parse<string>(this, read);

            SetValueFromPLC(parsed);

            return parsed;

        }

        private async Task CalibrateFromPLC () {

            Logger.Log($"Calibrating string ({PLCAddressName}) from PLC source", LogLevel.Verbose, attachedInterface);

            //get the string describer bytes
            var bytes = await attachedInterface.ReadByteRangeNonBlocking((int)MemoryAddress, 4, false);

            if (bytes == null || bytes.Length == 0 || bytes.All(x => x == 0x0)) {

                throw new MewtocolException($"The string register ({PLCAddressName}) doesn't exist in the PLC program");

            }

            ReservedSize = BitConverter.ToInt16(bytes, 0);
            UsedSize = BitConverter.ToInt16(bytes, 2);
            WordsSize = Math.Max(0, (uint)(2 + (ReservedSize + 1) / 2));

            CheckAddressOverflow(memoryAddress, WordsSize);

            isCalibratedFromPlc = true;

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            if (!attachedInterface.IsConnected) return false;

            if (!isCalibratedFromPlc) {

                //try to calibrate from plc
                await CalibrateFromPLC();

            }

            var encoded = PlcValueParser.Encode(this, (string)data);
            var res = await attachedInterface.WriteRawRegisterAsync(this, encoded);
            
            if (res) {
                SetValueFromPLC(data);
                UsedSize = (short)((string)Value).Length;
            }
            
            return res;

        }

    }

}
