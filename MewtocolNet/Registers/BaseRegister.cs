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
        internal object lastValue;
        internal Type collectionType;
        internal string name;
        internal int memoryAddress;

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
        public string PLCAddressName => GetRegisterPLCName();

        /// <inheritdoc/>
        public int MemoryAddress => memoryAddress;

        #region Trigger update notify

        public event PropertyChangedEventHandler PropertyChanged;

        internal void TriggerChangedEvnt(object changed) => ValueChanged?.Invoke(changed);

        public void TriggerNotifyChange() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));

        #endregion

        public virtual void ClearValue() => SetValueFromPLC(null);

        public virtual void SetValueFromPLC(object val) {

            lastValue = val;
            TriggerChangedEvnt(this);
            TriggerNotifyChange();

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

        public virtual string GetRegisterPLCName() => $"{GetRegisterString()}{MemoryAddress}";

        #endregion

        #region Read / Write

        public virtual async Task<object> ReadAsync() => throw new NotImplementedException();

        public virtual async Task<bool> WriteAsync(object data) => throw new NotImplementedException();

        #endregion

        public override string ToString() => $"{GetRegisterPLCName()}{(Name != null ? $" ({Name})" : "")} - Value: {GetValueString()}";

        public virtual string ToString(bool additional) {

            if (!additional) return this.ToString();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"PLC Naming: {GetRegisterPLCName()}");
            sb.AppendLine($"Name: {Name ?? "Not named"}");
            sb.AppendLine($"Value: {GetValueString()}");
            sb.AppendLine($"Register Type: {RegisterType}");
            sb.AppendLine($"Memory Address: {MemoryAddress}");

            return sb.ToString();

        }

    }

}
