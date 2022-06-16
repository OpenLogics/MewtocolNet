using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Responses {
    
    /// <summary>
    /// A class describing a register
    /// </summary>
    public abstract class Register : INotifyPropertyChanged {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event Action<object> ValueChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }
        public int MemoryAdress { get; set; }
        public int MemoryLength { get; set; }
        public virtual string BuildMewtocolIdent() {
            StringBuilder asciistring = new StringBuilder("D");
            asciistring.Append(MemoryAdress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAdress + MemoryLength).ToString().PadLeft(5, '0'));
            return asciistring.ToString();
        }
        protected void TriggerChangedEvnt(object changed) {
            ValueChanged?.Invoke(changed);
        }

        public void TriggerNotifyChange () {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
        }

        public string GetValueString () {

            if (this is NRegister<short> shortReg) {
                return shortReg.Value.ToString();
            }
            if (this is NRegister<ushort> ushortReg) {
                return ushortReg.Value.ToString();
            }
            if (this is NRegister<int> intReg) {
                return intReg.Value.ToString();
            }
            if (this is NRegister<uint> uintReg) {
                return uintReg.Value.ToString();
            }
            if (this is NRegister<float> floatReg) {
                return floatReg.Value.ToString();
            } 
            else if (this is SRegister stringReg) {
                return stringReg.Value.ToString();

            }

            return "Type of the register is not supported.";

        }

    }
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
        public NRegister(int _adress, string _name = null) {
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
        }

        public override string ToString() {
            return $"Adress: {MemoryAdress} Val: {Value}";
        }
    }

    /// <summary>
    /// Result for a read/write operation
    /// </summary>
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class NRegisterResult<T> {
        public CommandResult Result { get; set; }
        public NRegister<T> Register { get; set; }

        public override string ToString() {
            string errmsg = Result.Success ? "" : $", Error [{Result.ErrorDescription}]";
            return $"Result [{Result.Success}], Register [{Register.ToString()}]{errmsg}";
        }
    }

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
            MemoryLength = 1 + (_reservedStringSize) / 2;
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

        public void SetValueFromPLC (string val) {
            lastVal = val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();
        }


    }

    public class SRegisterResult {
        public CommandResult Result { get; set; }
        public SRegister Register { get; set; }

        public override string ToString() {
            string errmsg = Result.Success ? "" : $", Error [{Result.ErrorDescription}]";
            return $"Result [{Result.Success}], Register [{Register.ToString()}]{errmsg}";
        }
    }
}
