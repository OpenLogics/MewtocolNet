using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    public abstract class BaseRegister : IRegister, IRegisterInternal, INotifyPropertyChanged {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event Action<object> ValueChanged;

        internal MewtocolInterface attachedInterface;
        internal object lastValue = null;
        internal Type collectionType;
        internal string name;
        internal uint memoryAddress;

        /// <inheritdoc/>
        public MewtocolInterface AttachedInterface => attachedInterface;

        /// <inheritdoc/>
        public object Value => lastValue;

        /// <inheritdoc/>
        public RegisterType RegisterType { get; protected set; }

        /// <inheritdoc/>
        public Type CollectionType => collectionType;

        /// <inheritdoc/>
        public string Name => name;

        /// <inheritdoc/>
        public string PLCAddressName => GetMewName();

        /// <inheritdoc/>
        public uint MemoryAddress => memoryAddress;

        #region Trigger update notify

        public event PropertyChangedEventHandler PropertyChanged;

        public void TriggerNotifyChange() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));

        #endregion

        public virtual void ClearValue() => SetValueFromPLC(null);

        public virtual void SetValueFromPLC(object val) {

            if(lastValue?.ToString() != val?.ToString()) {

                lastValue = val;

                TriggerNotifyChange();
                attachedInterface.InvokeRegisterChanged(this);

            }

        }

        public void WithCollectionType(Type colType) => collectionType = colType;

        #region Default accessors

        public Type GetCollectionType() => CollectionType;

        public RegisterType GetRegisterType() => RegisterType;

        public virtual string BuildMewtocolQuery() {

            StringBuilder asciistring = new StringBuilder("D");
            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));
            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));
            return asciistring.ToString();

        }

        public virtual string GetStartingMemoryArea() => MemoryAddress.ToString();

        public virtual byte? GetSpecialAddress() => null;

        public virtual string GetValueString() => Value?.ToString() ?? "null";

        public virtual string GetAsPLC () => Value?.ToString() ?? "null";     

        public virtual string GetRegisterString() => RegisterType.ToString();

        public virtual string GetCombinedName() => $"{(CollectionType != null ? $"{CollectionType.Name}." : "")}{Name ?? "Unnamed"}";

        public virtual string GetContainerName() => $"{(CollectionType != null ? $"{CollectionType.Name}" : "")}";

        public virtual string GetMewName() => $"{GetRegisterString()}{MemoryAddress}";

        public virtual uint GetRegisterAddressLen() => throw new NotImplementedException();

        public string GetRegisterWordRangeString() => $"{GetMewName()} - {MemoryAddress + GetRegisterAddressLen() - 1}"; 

        #endregion

        #region Read / Write

        public virtual async Task<object> ReadAsync() => throw new NotImplementedException();

        public virtual async Task<bool> WriteAsync(object data) => throw new NotImplementedException();

        #endregion

        protected virtual void CheckAddressOverflow (uint addressStart, uint addressLen) {

            if (addressStart < 0)
                throw new NotSupportedException("The area address can't be negative");

            if (addressStart + addressLen > 99999)
                throw new NotSupportedException($"Memory adresses cant be greater than 99999 (DT{addressStart}-{addressStart + addressLen})");

        }

        public override string ToString() => $"{GetMewName()}{(Name != null ? $" ({Name})" : "")} - Value: {GetValueString()}";

        public virtual string ToString(bool additional) {

            if (!additional) return this.ToString();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"PLC Naming: {GetMewName()}");
            sb.AppendLine($"Name: {Name ?? "Not named"}");
            sb.AppendLine($"Value: {GetValueString()}");
            sb.AppendLine($"Register Type: {RegisterType}");
            sb.AppendLine($"Memory Address: {MemoryAddress}");

            return sb.ToString();

        }

    }

}
