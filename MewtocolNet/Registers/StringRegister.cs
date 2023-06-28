using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        internal int WordsSize { get; set; }  

        private bool isCalibrated = false;

        /// <summary>
        /// Defines a register containing a string
        /// </summary>
        public StringRegister (int _address, string _name = null) {

            if (_address > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");
            name = _name;
            memoryAddress = _address;
            RegisterType = RegisterType.DT_BYTE_RANGE;

        }

        /// <inheritdoc/>
        public override string BuildMewtocolQuery() {

            StringBuilder asciistring = new StringBuilder("D");

            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAddress + WordsSize - 1).ToString().PadLeft(5, '0'));

            return asciistring.ToString();
        }

        /// <inheritdoc/>
        public override string GetValueString() => $"'{Value}'";

        /// <inheritdoc/>
        public override void SetValueFromPLC (object val) {

            lastValue = (string)val;

            TriggerChangedEvnt(this);
            TriggerNotifyChange();

        }

        /// <inheritdoc/>
        public override string GetRegisterString() => "DT";

        /// <inheritdoc/>
        public override void ClearValue() => SetValueFromPLC("");

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            if (!attachedInterface.IsConnected) return null;

            //get the string params first

            if(!isCalibrated) await Calibrate();

            var read = await attachedInterface.ReadRawRegisterAsync(this);
            if (read == null) return null;

            return PlcValueParser.Parse<string>(this, read);

        }

        private async Task Calibrate () {

            //get the string describer bytes
            var bytes = await attachedInterface.ReadByteRange(MemoryAddress, 4, false);

            ReservedSize = BitConverter.ToInt16(bytes, 0);
            UsedSize = BitConverter.ToInt16(bytes, 2);
            WordsSize = 2 + (ReservedSize + 1) / 2;

            isCalibrated = true;

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            if (!attachedInterface.IsConnected) return false;

            var res = await attachedInterface.WriteRawRegisterAsync(this, PlcValueParser.Encode(this, (string)data));

            if (res) UsedSize = (short)((string)Value).Length;

            return res;

        }

    }

}
