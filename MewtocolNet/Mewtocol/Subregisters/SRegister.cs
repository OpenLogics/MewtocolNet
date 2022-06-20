using System;
using System.Text;

namespace MewtocolNet.Responses {
    /// <summary>
    /// Defines a register containing a string
    /// </summary>
    public class SRegister : Register {

        private string lastVal = "";
        public string Value {

            get => lastVal;
            
        }

        public short ReservedSize { get; set; }

        /// <summary>
        /// Defines a register containing a string
        /// </summary>
        public SRegister(int _adress, int _reservedStringSize, string _name = null) {

            if (_adress > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");
            Name = _name;
            MemoryAdress = _adress;
            ReservedSize = (short)_reservedStringSize;

            //calc mem length
            var wordsize = (double)_reservedStringSize / 2;
            if (wordsize % 2 != 0) {
                wordsize++;
            }

            MemoryLength = (int)Math.Round(wordsize + 1);
        }

        public override string ToString() {
            return $"Adress: {MemoryAdress} Val: {Value}";
        }

        public override string BuildMewtocolIdent() {

            StringBuilder asciistring = new StringBuilder("D");

            asciistring.Append(MemoryAdress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAdress + MemoryLength).ToString().PadLeft(5, '0'));

            return asciistring.ToString();
        }

        internal string BuildCustomIdent (int overwriteWordLength) {

            if (overwriteWordLength <= 0)
                throw new Exception("overwriteWordLength cant be 0 or less");

            StringBuilder asciistring = new StringBuilder("D");

            asciistring.Append(MemoryAdress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAdress + overwriteWordLength - 1).ToString().PadLeft(5, '0'));

            return asciistring.ToString();
        }

        public void SetValueFromPLC (string val) {
            lastVal = val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();
        }


    }

}
