using System;
using System.ComponentModel;
using System.Text;

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

        internal int memoryAddress;
        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public int MemoryAddress => memoryAddress;

        internal byte specialAddress;
        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public byte SpecialAddress => specialAddress;

        /// <summary>
        /// Creates a new boolean register
        /// </summary>
        /// <param name="_io">The io type prefix</param>
        /// <param name="_spAddress">The special address</param>
        /// <param name="_areaAdress">The area special address</param>
        /// <param name="_name">The custom name</param>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="Exception"></exception>
        public BRegister(IOType _io, byte _spAddress = 0x0, int _areaAdress = 0, string _name = null) {

            if (_areaAdress < 0)
                throw new NotSupportedException("The area address cant be negative");

            if (_io == IOType.R && _areaAdress >= 512)
                throw new NotSupportedException("R area addresses cant be greater than 511");

            if ((_io == IOType.X || _io == IOType.Y) && _areaAdress >= 110)
                throw new NotSupportedException("XY area addresses cant be greater than 110");

            if (_spAddress > 0xF)
                throw new NotSupportedException("Special address cant be greater 15 or 0xF");

            memoryAddress = _areaAdress;
            specialAddress = _spAddress;
            name = _name;

            RegType = (RegisterType)(int)_io;

        }

        internal BRegister WithCollectionType(Type colType) {

            collectionType = colType;
            return this;

        }

        /// <summary>
        /// Builds the register area name
        /// </summary>
        public string BuildMewtocolQuery() {

            //build area code from register type
            StringBuilder asciistring = new StringBuilder(RegType.ToString());

            string memPadded = MemoryAddress.ToString().PadLeft(4, '0');
            string sp = SpecialAddress.ToString("X1");

            asciistring.Append(memPadded);
            asciistring.Append(sp);

            return asciistring.ToString();

        }

        internal void SetValueFromPLC(bool val) {

            lastValue = val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();

        }

        public string GetStartingMemoryArea() {

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

            var spAdressEnd = SpecialAddress.ToString("X1");

            if (MemoryAddress == 0) {

                return $"{GetRegisterString()}{spAdressEnd}";

            }

            if (MemoryAddress > 0 && SpecialAddress != 0) {

                return $"{GetRegisterString()}{MemoryAddress}{spAdressEnd}";

            }

            return $"{GetRegisterString()}{MemoryAddress}";

        }

        internal void TriggerChangedEvnt(object changed) => ValueChanged?.Invoke(changed);

        public void TriggerNotifyChange() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));

        public override string ToString() => $"{GetRegisterPLCName()} - Value: {GetValueString()}";

    }

}
