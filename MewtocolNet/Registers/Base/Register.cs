﻿using MewtocolNet.Events;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.UnderlyingRegisters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    public abstract class Register : IRegister, INotifyPropertyChanged {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event RegisterChangedEventHandler ValueChanged;

        //links to 
        internal RegisterCollection containedCollection;
        internal MewtocolInterface attachedInterface;

        internal List<RegisterPropTarget> boundProperties = new List<RegisterPropTarget>();

        internal Type underlyingSystemType;
        internal IMemoryArea underlyingMemory;
        internal bool autoGenerated;
        internal bool isAnonymous;

        internal protected object lastValue = null;
        internal string name;
        internal uint memoryAddress;
        internal int pollLevel = 0;

        internal uint successfulReads = 0;
        internal uint successfulWrites = 0;

        internal bool wasOverlapFitted = false;

        /// <inheritdoc/>
        internal RegisterCollection ContainedCollection => containedCollection;

        /// <inheritdoc/>
        internal MewtocolInterface AttachedInterface => attachedInterface;

        /// <inheritdoc/>
        public Type UnderlyingSystemType => underlyingSystemType;

        /// <inheritdoc/>
        public object ValueObj => lastValue;

        /// <inheritdoc/>
        public string ValueStr => GetValueString();

        /// <inheritdoc/>
        public RegisterPrefix RegisterType { get; internal set; }

        /// <inheritdoc/>
        public string Name => name;

        /// <inheritdoc/>
        public string PLCAddressName => GetMewName();

        /// <inheritdoc/>
        public uint MemoryAddress => memoryAddress;

        /// <inheritdoc/>
        int IRegister.PollLevel => pollLevel;

        #region Trigger update notify

        public event PropertyChangedEventHandler PropertyChanged;

        public void TriggerNotifyChange() {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueObj)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueStr)));

        }

        #endregion

        public virtual void ClearValue() => UpdateHoldingValue(null);

        internal virtual void UpdateHoldingValue(object val) {

            bool nullDiff = false;
            if (val == null && lastValue != null) nullDiff = true;
            if (val != null && lastValue == null) nullDiff = true;

            if (lastValue?.ToString() != val?.ToString() || nullDiff) {

                var beforeVal = lastValue;
                var beforeValStr = GetValueString();

                lastValue = val;

                TriggerNotifyChange();
                attachedInterface.InvokeRegisterChanged(this, beforeVal, beforeValStr);

            }

        }

        internal virtual object SetValueFromBytes(byte[] bytes) => throw new NotImplementedException();

        internal void WithRegisterCollection(RegisterCollection collection) => containedCollection = collection;

        internal void WithBoundProperty(RegisterPropTarget propInfo) => boundProperties.Add(propInfo);

        internal void WithBoundProperties(IEnumerable<RegisterPropTarget> propInfos) {

            foreach (var item in propInfos)
                boundProperties.Add(item);

        }

        #region Default accessors

        public virtual byte? GetSpecialAddress() => null;

        public virtual string GetValueString() => ValueObj?.ToString() ?? "null";

        public virtual string GetAsPLC() => ValueObj?.ToString() ?? "null";

        public virtual string GetRegisterString() => RegisterType.ToString();

        public virtual string GetCombinedName() => $"{GetContainerName()}{(GetContainerName() != null ? "." : "")}{Name ?? "Unnamed"}";

        public virtual string GetContainerName() => $"{(containedCollection != null ? $"{containedCollection.GetType().Name}" : null)}";

        public virtual string GetMewName() => $"{GetRegisterString()}{MemoryAddress}";

        public virtual uint GetRegisterAddressLen() => throw new NotImplementedException();

        public virtual uint GetRegisterAddressEnd() => MemoryAddress + GetRegisterAddressLen() - 1;

        public string GetRegisterWordRangeString() => $"{GetMewName()} - {MemoryAddress + GetRegisterAddressLen() - 1}";

        #endregion

        protected virtual void CheckAddressOverflow(uint addressStart, uint addressLen) {

            if (addressStart < 0)
                throw new NotSupportedException("The area address can't be negative");

            if (addressStart + addressLen > 99999)
                throw new NotSupportedException($"Memory adresses cant be greater than 99999 (DT{addressStart}-{addressStart + addressLen})");

        }

        protected virtual void AddSuccessRead() {
            if (successfulReads == uint.MaxValue) successfulReads = 0;
            else successfulReads++;
        }

        protected virtual void AddSuccessWrite() {
            if (successfulWrites == uint.MaxValue) successfulWrites = 0;
            else successfulWrites++;
        }

        internal virtual bool IsSameAddressAndType(Register toCompare) {

            return this.MemoryAddress == toCompare.MemoryAddress &&
                   this.RegisterType == toCompare.RegisterType &&
                   this.underlyingSystemType == toCompare.underlyingSystemType &&
                   this.GetRegisterAddressLen() == toCompare.GetRegisterAddressLen() &&
                   this.GetSpecialAddress() == toCompare.GetSpecialAddress();

        }

        internal int AveragePollLevel(List<Register> testAgainst, PollLevelOverwriteMode mode) {

            var whereAddressFitsInto = this.CanFitAddressRange(testAgainst)
                                           .Where(x => !x.wasOverlapFitted).ToList();

            this.wasOverlapFitted = true;
            if (whereAddressFitsInto.Count == 0) return this.pollLevel;

            whereAddressFitsInto.Add(this);

            int avgLvl = mode == PollLevelOverwriteMode.Highest ?
            whereAddressFitsInto.Max(x => x.pollLevel) : whereAddressFitsInto.Min(x => x.pollLevel);

            whereAddressFitsInto.ForEach(x => x.pollLevel = avgLvl);

            return avgLvl;

        }

        internal IEnumerable<Register> CanFitAddressRange(List<Register> testAgainst) {

            foreach (var reg in testAgainst) {

                if (reg == this) continue;

                bool otherFitsInsideSelf = (reg.MemoryAddress >= this.MemoryAddress) &&
                                           (reg.GetRegisterAddressEnd() <= this.GetRegisterAddressEnd()) &&
                                           (reg.GetSpecialAddress() == this.GetSpecialAddress());

                if (otherFitsInsideSelf) yield return reg;

            }

        }

        public override string ToString() {

            var sb = new StringBuilder();
            sb.Append(GetRegisterWordRangeString());
            if (Name != null) sb.Append($" ({Name})");
            sb.Append($" [{this.GetType().Name}({underlyingSystemType.Name})]");
            if (ValueObj != null) sb.Append($" Val: {GetValueString()}");

            return sb.ToString();

        }

        public virtual string ToString(bool additional) {

            if (!additional) return this.ToString();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"MewName: {GetMewName()}");
            sb.AppendLine($"Name: {Name ?? "Not named"}");
            sb.AppendLine($"Value: {GetValueString()}");
            sb.AppendLine($"Register Type: {RegisterType}");
            sb.AppendLine($"Address: {GetRegisterWordRangeString()}");

            return sb.ToString();

        }

        public virtual string Explain() {

            StringBuilder sb = new StringBuilder();
            sb.Append($"Address: {GetRegisterWordRangeString()}\n");

            if (GetType().IsGenericType)
                sb.Append($"Type: {RegisterType}, {GetType().Name}<{GetType().GenericTypeArguments[0]}>\n");
            else
                sb.AppendLine($"Type: {RegisterType}, {GetType().Name}\n");

            sb.Append($"Name: {Name ?? "Not named"}\n");

            if (ValueObj != null)
                sb.Append($"Value: {GetValueString()}\n");

            sb.Append($"Reads: {successfulReads}, Writes: {successfulWrites}\n");

            if (GetSpecialAddress() != null)
                sb.Append($"SPAddress: {GetSpecialAddress():X1}\n");

            if (containedCollection != null)
                sb.Append($"In collection: {containedCollection.GetType()}\n");

            if (boundProperties != null && boundProperties.Count > 0)
                sb.Append($"Bound props: \n\t{string.Join(",\n\t", boundProperties)}");
            else
                sb.Append("No bound properties");

            return sb.ToString();

        }

    }

}
