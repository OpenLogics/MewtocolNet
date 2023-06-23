using System;
using System.ComponentModel;
using System.Text;

namespace MewtocolNet.Subregisters {
    /// <summary>
    /// Defines a register containing a string
    /// </summary>
    public class SRegister : IRegister {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event Action<object> ValueChanged;

        /// <summary>
        /// Triggers when a property on the register changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        internal Type collectionType;

        /// <summary>
        /// The type of collection the register is in or null of added manually
        /// </summary>
        public Type CollectionType => collectionType;

        internal string lastValue;

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

        internal int memoryLength;
        /// <summary>
        /// The registers memory length
        /// </summary>
        public int MemoryLength => memoryLength;

        internal short ReservedSize { get; set; }

        /// <summary>
        /// Defines a register containing a string
        /// </summary>
        public SRegister(int _adress, int _reservedStringSize, string _name = null) {

            if (_adress > 99999) throw new NotSupportedException("Memory adresses cant be greater than 99999");
            name = _name;
            memoryAdress = _adress;
            ReservedSize = (short)_reservedStringSize;

            //calc mem length
            var wordsize = (double)_reservedStringSize / 2;
            if (wordsize % 2 != 0) {
                wordsize++;
            }

            memoryLength = (int)Math.Round(wordsize + 1);
        }

        internal SRegister WithCollectionType(Type colType) {

            collectionType = colType;
            return this;

        }

        /// <summary>
        /// Builds the register identifier for the mewotocol protocol
        /// </summary>
        public string BuildMewtocolQuery() {

            StringBuilder asciistring = new StringBuilder("D");

            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAddress + MemoryLength).ToString().PadLeft(5, '0'));

            return asciistring.ToString();
        }

        internal string BuildCustomIdent(int overwriteWordLength) {

            if (overwriteWordLength <= 0)
                throw new Exception("overwriteWordLength cant be 0 or less");

            StringBuilder asciistring = new StringBuilder("D");

            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAddress + overwriteWordLength - 1).ToString().PadLeft(5, '0'));

            return asciistring.ToString();
        }

        public Type GetCollectionType() => CollectionType;

        public bool IsUsedBitwise() => false;

        internal void SetValueFromPLC(string val) {

            lastValue = val;

            TriggerChangedEvnt(this);
            TriggerNotifyChange();

        }

        public string GetStartingMemoryArea() => MemoryAddress.ToString();

        public string GetValueString() => Value?.ToString() ?? "";

        public string GetRegisterString() => "DT";

        public string GetCombinedName() => $"{(CollectionType != null ? $"{CollectionType.Name}." : "")}{Name ?? "Unnamed"}";

        public string GetContainerName() => $"{(CollectionType != null ? $"{CollectionType.Name}" : "")}";

        public string GetRegisterPLCName() => $"{GetRegisterString()}{MemoryAddress}";

        public void ClearValue() => SetValueFromPLC(null);

        internal void TriggerChangedEvnt(object changed) => ValueChanged?.Invoke(changed);

        public void TriggerNotifyChange() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));

        public override string ToString() => $"{GetRegisterPLCName()} - Value: {GetValueString()}";

    }

}
