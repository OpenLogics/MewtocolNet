using System;
using System.ComponentModel;
using System.Text;
using MewtocolNet;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a boolean
    /// </summary>
    public class BRegister : IRegister, INotifyPropertyChanged {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event Action<object> ValueChanged;

        /// <summary>
        /// Triggers when a property on the register changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        internal RegisterType RegType { get; private set; }     

        internal SpecialAddress SpecialAddress { get; private set; }

        internal Type collectionType;

        /// <summary>
        /// The type of collection the register is in or null of added manually
        /// </summary>
        public Type CollectionType => collectionType;

        internal bool lastValue;

        /// <summary>
        /// The value of the register
        /// </summary>
        public object Value => lastValue;

        internal string name;
        /// <summary>
        /// The register name or null of not defined
        /// </summary>
        public string Name => name;

        internal int memoryAdress;
        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public int MemoryAddress => memoryAdress;

        /// <summary>
        /// Defines a register containing a number
        /// </summary>
        /// <param name="_address">Memory start adress max 99999</param>
        /// <param name="_type">Type of boolean register</param>
        /// <param name="_name">Name of the register</param>
        public BRegister (int _address, RegisterType _type = RegisterType.R, string _name = null) {

            if (_address > 99999) throw new NotSupportedException("Memory addresses cant be greater than 99999");

            if (_type != RegisterType.X && _type != RegisterType.Y && _type != RegisterType.R)
                throw new NotSupportedException("The register type cant be numeric, use X, Y or R");

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
                throw new NotSupportedException("Special address cant be none");

            if (_type != RegisterType.X && _type != RegisterType.Y && _type != RegisterType.R)
                throw new NotSupportedException("The register type cant be numeric, use X, Y or R");

            SpecialAddress = _address;
            name = _name;

            RegType = _type;

        }

        internal BRegister WithCollectionType(Type colType) {

            collectionType = colType;
            return this;

        }

        /// <summary>
        /// Builds the register area name
        /// </summary>
        public string BuildMewtocolQuery () {

            //build area code from register type
            StringBuilder asciistring = new StringBuilder(RegType.ToString());
            if(SpecialAddress == SpecialAddress.None) {
                asciistring.Append(MemoryAddress.ToString().PadLeft(4, '0'));
            } else {
                asciistring.Append(SpecialAddress.ToString().PadLeft(4, '0'));
            }
            
            return asciistring.ToString();

        }

        internal void SetValueFromPLC (bool val) {

            lastValue = val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();
        
        }

        public string GetStartingMemoryArea() {

            if (SpecialAddress != SpecialAddress.None)
                return SpecialAddress.ToString();

            return MemoryAddress.ToString();

        }

        public bool IsUsedBitwise() => false;

        public Type GetCollectionType() => CollectionType;

        public string GetValueString() => Value.ToString();

        public void ClearValue() => SetValueFromPLC(false);

        public string GetRegisterString() => RegType.ToString();

        public string GetCombinedName() => $"{(CollectionType != null ? $"{CollectionType.Name}." : "")}{Name ?? "Unnamed"}";

        public string GetContainerName() => $"{(CollectionType != null ? $"{CollectionType.Name}" : "")}";

        public string GetRegisterPLCName() {

            if (SpecialAddress != SpecialAddress.None) {
                return $"{GetRegisterString()}{SpecialAddress}";
            }

            return $"{GetRegisterString()}{MemoryAddress}";

        }

        internal void TriggerChangedEvnt(object changed) => ValueChanged?.Invoke(changed);

        public void TriggerNotifyChange() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));

        public override string ToString() => $"{GetRegisterPLCName()} - Value: {GetValueString()}";

    }

}
