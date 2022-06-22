using System;
using System.Text;
using MewtocolNet;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a boolean
    /// </summary>
    public class BRegister : Register {

        internal RegisterType RegType { get; private set; }     
        internal SpecialAddress SpecialAddress { get; private set; }        

        public bool NeedValue;
        public bool LastValue;

        /// <summary>
        /// The value of the register
        /// </summary>
        public bool Value => LastValue;

        /// <summary>
        /// Defines a register containing a number
        /// </summary>
        /// <param name="_address">Memory start adress max 99999</param>
        /// <param name="_type">Type of boolean register</param>
        /// <param name="_name">Name of the register</param>
        public BRegister (int _address, RegisterType _type = RegisterType.R, string _name = null) {

            if (_address > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");
            memoryAdress = _address;
            name = _name;

            RegType = _type;

        }

        /// <summary>
        /// Defines a register containing a number
        /// </summary>
        /// <param name="_address">Memory start adress max 99999</param>
        /// <param name="_type">Type of boolean register</param>
        /// <param name="_name">Name of the register</param>
        public BRegister (SpecialAddress _address, RegisterType _type = RegisterType.R, string _name = null) {

            if (_address == SpecialAddress.None)
                throw new NotSupportedException("Special adress cant be none");

            SpecialAddress = _address;
            name = _name;

            RegType = _type;

        }

        public override string BuildMewtocolIdent () {

            //build area code from register type
            StringBuilder asciistring = new StringBuilder(RegType.ToString());
            if(SpecialAddress == SpecialAddress.None) {
                asciistring.Append(MemoryAdress.ToString().PadLeft(4, '0'));
            } else {
                asciistring.Append(SpecialAddress.ToString().PadLeft(4, '0'));
            }
            
            return asciistring.ToString();

        }

        internal void SetValueFromPLC (bool val) {
            LastValue = val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();
        }
        public override string ToString() {
            return $"Adress: {MemoryAdress} Val: {Value}";
        }
    }

}
