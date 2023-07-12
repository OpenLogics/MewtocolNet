using MewtocolNet.RegisterAttributes;
using MewtocolNet.UnderlyingRegisters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    public abstract class BaseRegister : IRegister, IRegisterInternal, INotifyPropertyChanged {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event Action<object> ValueChanged;

        //links to 
        internal RegisterCollection containedCollection;
        internal MewtocolInterface attachedInterface;
        internal List<RegisterPropTarget> boundToProps = new List<RegisterPropTarget>();

        internal IMemoryArea underlyingMemory;
        internal object lastValue = null;
        internal string name;
        internal uint memoryAddress;
        internal int pollLevel = 0;

        internal uint successfulReads = 0;
        internal uint successfulWrites = 0;

        /// <inheritdoc/>
        public RegisterCollection ContainedCollection => containedCollection;   

        /// <inheritdoc/>
        public MewtocolInterface AttachedInterface => attachedInterface;

        /// <inheritdoc/>
        public object Value => lastValue;

        /// <inheritdoc/>
        public RegisterType RegisterType { get; protected set; }

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

        internal virtual object SetValueFromBytes(byte[] bytes) => throw new NotImplementedException();

        internal void WithRegisterCollection (RegisterCollection collection) => containedCollection = collection;

        internal void WithBoundProperty(RegisterPropTarget propInfo) => boundToProps.Add(propInfo);

        #region Read / Write

        public virtual Task<object> ReadAsync() => throw new NotImplementedException();

        public virtual Task<bool> WriteAsync(object data) => throw new NotImplementedException();

        internal virtual Task<bool> WriteToAnonymousAsync (object value) => throw new NotImplementedException();

        internal virtual Task<object> ReadFromAnonymousAsync () => throw new NotImplementedException();

        #endregion

        #region Default accessors

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

        public virtual string GetCombinedName() => $"{GetContainerName()}{(GetContainerName() != null ? "." : "")}{Name ?? "Unnamed"}";

        public virtual string GetContainerName() => $"{(containedCollection != null ? $"{containedCollection.GetType().Name}" : null)}";

        public virtual string GetMewName() => $"{GetRegisterString()}{MemoryAddress}";

        public virtual uint GetRegisterAddressLen() => throw new NotImplementedException();

        public string GetRegisterWordRangeString() => $"{GetMewName()} - {MemoryAddress + GetRegisterAddressLen() - 1}"; 

        #endregion

        protected virtual void CheckAddressOverflow (uint addressStart, uint addressLen) {

            if (addressStart < 0)
                throw new NotSupportedException("The area address can't be negative");

            if (addressStart + addressLen > 99999)
                throw new NotSupportedException($"Memory adresses cant be greater than 99999 (DT{addressStart}-{addressStart + addressLen})");

        }

        protected virtual void AddSuccessRead () {
            if (successfulReads == uint.MaxValue) successfulReads = 0;
            else successfulReads++;
        }

        protected virtual void AddSuccessWrite () {
            if (successfulWrites == uint.MaxValue) successfulWrites = 0;
            else successfulWrites++;
        }

        public override string ToString() {

            var sb = new StringBuilder();
            sb.Append(GetMewName());
            if(Name != null) sb.Append($" ({Name})");
            if (Value != null) sb.Append($" Val: {GetValueString()}");

            return sb.ToString();

        }

        public virtual string ToString(bool additional) {

            if (!additional) return this.ToString();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"MewName: {GetMewName()}");
            sb.AppendLine($"Name: {Name ?? "Not named"}");
            sb.AppendLine($"Value: {GetValueString()}");
            sb.AppendLine($"Perf. Reads: {successfulReads}, Writes: {successfulWrites}");
            sb.AppendLine($"Register Type: {RegisterType}");
            sb.AppendLine($"Address: {GetRegisterWordRangeString()}");
            if(this is StringRegister sr) sb.AppendLine($"Reserved: {sr.ReservedSize}, Used: {sr.UsedSize}");
            if (GetSpecialAddress() != null) sb.AppendLine($"SPAddress: {GetSpecialAddress():X1}");
            if (GetType().IsGenericType) sb.AppendLine($"Type: NumberRegister<{GetType().GenericTypeArguments[0]}>");
            else sb.AppendLine($"Type: {GetType()}");
            if(containedCollection != null) sb.AppendLine($"In collection: {containedCollection.GetType()}");
            if(boundToProps != null && boundToProps.Count != 0) 
                sb.AppendLine($"Bound props: {string.Join(",", boundToProps)}");

            return sb.ToString();

        }

    }

}
