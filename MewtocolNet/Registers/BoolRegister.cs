using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a boolean
    /// </summary>
    public class BoolRegister : IRegister, IRegisterInternal, INotifyPropertyChanged {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event Action<object> ValueChanged;

        /// <summary>
        /// Triggers when a property on the register changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public RegisterType RegisterType { get; private set; }

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
        public BoolRegister(IOType _io, byte _spAddress = 0x0, int _areaAdress = 0, string _name = null) {

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

            RegisterType = (RegisterType)(int)_io;

        }

        public void WithCollectionType (Type colType) => collectionType = colType;

        public byte? GetSpecialAddress() => SpecialAddress;

        /// <summary>
        /// Builds the register area name for the mewtocol protocol
        /// </summary>
        public string BuildMewtocolQuery() {

            //(R|X|Y)(area add [3] + special add [1])
            StringBuilder asciistring = new StringBuilder();

            string prefix = RegisterType.ToString();
            string mem = MemoryAddress.ToString();
            string sp = SpecialAddress.ToString("X1");

            asciistring.Append(prefix);
            asciistring.Append(mem.PadLeft(3, '0'));
            asciistring.Append(sp);

            return asciistring.ToString();

        }

        public void SetValueFromPLC(object val) {

            lastValue = (bool)val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();

        }

        public string GetStartingMemoryArea() {

            return MemoryAddress.ToString();

        }

        public bool IsUsedBitwise() => false;

        public Type GetCollectionType() => CollectionType;

        public RegisterType GetRegisterType() => RegisterType;

        public string GetValueString() => Value.ToString();

        public void ClearValue() => SetValueFromPLC(false);

        public string GetRegisterString() => RegisterType.ToString();

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

        public string ToString(bool additional) {

            if (!additional) return this.ToString();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"PLC Naming: {GetRegisterPLCName()}");
            sb.AppendLine($"Name: {Name ?? "Not named"}");
            sb.AppendLine($"Value: {GetValueString()}");
            sb.AppendLine($"Register Type: {RegisterType}");
            sb.AppendLine($"Memory Address: {MemoryAddress}");
            sb.AppendLine($"Special Address: {SpecialAddress:X1}");

            return sb.ToString();

        }

        public async Task<object> ReadAsync (MewtocolInterface interf) {

            var read = await interf.ReadRawRegisterAsync(this);
            return PlcValueParser.Parse<bool>(read);

        }

        public async Task<bool> WriteAsync (MewtocolInterface interf, object data) {

            return await interf.WriteRawRegisterAsync(this, PlcValueParser.Encode((bool)data));

        }

    }

}
