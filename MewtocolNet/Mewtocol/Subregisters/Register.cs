using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// A class describing a register
    /// </summary>
    public abstract class Register : INotifyPropertyChanged {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event Action<object> ValueChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The register name or null of not defined
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public int MemoryAdress { get; set; }
        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public int MemoryLength { get; set; }

        /// <summary>
        /// The value of the register auto converted to a string
        /// </summary>
        public string StringValue => GetValueString();

        /// <summary>
        /// The name the register would have in the PLC
        /// </summary>
        public string RegisterPLCName => GetRegisterPLCName();

        internal bool isUsedBitwise { get; set; }    

        internal Register () {
            ValueChanged += (obj) => {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StringValue)));
            };
        }

        public virtual string BuildMewtocolIdent() {

            StringBuilder asciistring = new StringBuilder("D");
            asciistring.Append(MemoryAdress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAdress + MemoryLength).ToString().PadLeft(5, '0'));
            return asciistring.ToString();

        }
        internal void TriggerChangedEvnt(object changed) {
            ValueChanged?.Invoke(changed);
        }

        internal void TriggerNotifyChange () {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
        }

        /// <summary>
        /// Gets the starting memory are either numeric or A,B,C,D etc for special areas like inputs
        /// </summary>
        /// <returns></returns>
        public string GetStartingMemoryArea () {

            if (this is BRegister bReg && bReg.SpecialAddress != SpecialAddress.None) {
                return bReg.SpecialAddress.ToString();
            }

            return this.MemoryAdress.ToString();

        }

        /// <summary>
        /// Gets the current value in the adress as a string 
        /// </summary>
        /// <returns></returns>
        public string GetValueString () {

            if (this is NRegister<short> shortReg) {
                return $"{shortReg.Value}{(isUsedBitwise ? $" [{shortReg.GetBitwise().ToBitString()}]" : "")}";
            }
            if (this is NRegister<ushort> ushortReg) {
                return ushortReg.Value.ToString();
            }
            if (this is NRegister<int> intReg) {
                return $"{intReg.Value}{(isUsedBitwise ? $" [{intReg.GetBitwise().ToBitString()}]" : "")}";
            }
            if (this is NRegister<uint> uintReg) {
                return uintReg.Value.ToString();
            }
            if (this is NRegister<float> floatReg) {
                return floatReg.Value.ToString();
            }
            if (this is NRegister<TimeSpan> tsReg) {
                return tsReg.Value.ToString();
            }
            if (this is BRegister boolReg) {
                return boolReg.Value.ToString();
            }
            if (this is SRegister stringReg) {
                return stringReg.Value.ToString();

            }

            return "Type of the register is not supported.";

        }

        /// <summary>
        /// Gets the register bitwise if its a 16 or 32 bit int
        /// </summary>
        /// <returns>A bitarray</returns>
        public BitArray GetBitwise () {

            if (this is NRegister<short> shortReg) {

                var bytes = BitConverter.GetBytes(shortReg.Value);
                BitArray bitAr = new BitArray(bytes);
                return bitAr;

            }

            if (this is NRegister<int> intReg) {

                var bytes = BitConverter.GetBytes(intReg.Value);
                BitArray bitAr = new BitArray(bytes);
                return bitAr;

            }

            return null;

        }

        public string GetRegisterString () {

            if (this is NRegister<short> shortReg) {
                return "DT";
            }
            if (this is NRegister<ushort> ushortReg) {
                return "DT";
            }
            if (this is NRegister<int> intReg) {
                return "DDT";
            }
            if (this is NRegister<uint> uintReg) {
                return "DDT";
            }
            if (this is NRegister<float> floatReg) {
                return "DDT";
            }
            if (this is NRegister<TimeSpan> tsReg) {
                return "DDT";
            }
            if (this is BRegister boolReg) {
                return boolReg.RegType.ToString();  
            }
            if (this is SRegister stringReg) {
                return "DT";

            }

            return "Type of the register is not supported.";

        }

        public string GetRegisterPLCName () {

            return $"{GetRegisterString()}{MemoryAdress}";

        }

    }

}
