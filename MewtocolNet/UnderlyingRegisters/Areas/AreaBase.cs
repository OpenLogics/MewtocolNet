using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {

    internal class AreaBase : IMemoryArea {

        private MewtocolInterface mewInterface;
        private int pollLevel;

        internal RegisterPrefix registerType;

        internal ulong addressStart;
        internal ulong addressEnd;

        internal byte[] underlyingBytes = new byte[2];

        /// <summary>
        /// List of register link groups that are managed in this memory area
        /// </summary>
        internal List<LinkedRegisterGroup> managedRegisters = new List<LinkedRegisterGroup>();

        public event PropertyChangedEventHandler PropertyChanged;

        public ulong AddressStart => addressStart;
        public ulong AddressEnd => addressEnd;

        //interface

        public string AddressRange => GetAddressRangeString();

        public IReadOnlyList<Word> UnderlyingWords => GetUnderlyingWords();

        public string UnderlyingWordsString => string.Join(" ", GetUnderlyingWords());

        public int PollLevel => pollLevel;

        internal AreaBase(MewtocolInterface mewIf, int pollLvl) {

            mewInterface = mewIf;
            pollLevel = pollLvl;

        }

        internal void BoundaryUdpdate(uint? addrFrom = null, uint? addrTo = null) {

            var addFrom = addrFrom ?? addressStart;
            var addTo = addrTo ?? addressEnd;

            var oldFrom = addressStart;
            var oldUnderlying = underlyingBytes.ToArray();

            underlyingBytes = new byte[(addTo + 1 - addFrom) * 2];

            //copy old bytes to new array
            var offset = (int)(oldFrom - addFrom) * 2;
            oldUnderlying.CopyTo(underlyingBytes, offset);

            addressStart = addFrom;
            addressEnd = addTo;

            OnPropChange(nameof(AddressRange));
            OnPropChange(nameof(UnderlyingWords));

        }

        public void UpdateAreaRegisterValues() {

            foreach (var register in this.managedRegisters.SelectMany(x => x.Linked)) {

                var regStart = register.MemoryAddress;
                var addLen = (int)register.GetRegisterAddressLen();

                var bytes = this.GetUnderlyingBytes(regStart, addLen);
                register.SetValueFromBytes(bytes);

                OnPropChange(nameof(UnderlyingWords));
                OnPropChange(nameof(UnderlyingWordsString));

            }

        }

        internal async Task<bool> RequestByteReadAsync(ulong addStart, ulong addEnd) {

            var byteCount = (addEnd - addStart + 1) * 2;
            var result = await mewInterface.ReadAreaByteRangeAsync((int)addStart, (int)byteCount, registerType);

            if (result != null) {

                SetUnderlyingBytes(result, addStart);
                return true;

            }

            return false;

        }

        public byte[] GetUnderlyingBytes(Register reg) {

            int byteLen = (int)(reg.GetRegisterAddressLen() * 2);

            return GetUnderlyingBytes(reg.MemoryAddress, byteLen);

        }

        internal byte[] GetUnderlyingBytes(uint addStart, int addLen) {

            int byteLen = (int)(addLen * 2);

            int copyOffset = (int)((addStart - addressStart) * 2);
            var gotBytes = underlyingBytes.Skip(copyOffset).Take(byteLen).ToArray();

            return gotBytes;

        }

        public void SetUnderlyingBytes(Register reg, byte[] bytes) {

            SetUnderlyingBytes(bytes, reg.MemoryAddress);

        }

        public void SetUnderlyingBits(Register reg, int bitIndex, bool value) {

            var underlyingBefore = GetUnderlyingBytes(reg);

            var bitArr = new BitArray(underlyingBefore);

            bitArr[bitIndex] = value;   

            bitArr.CopyTo(underlyingBefore, 0);

            SetUnderlyingBytes(underlyingBefore, reg.MemoryAddress);

        }

        private void SetUnderlyingBytes(byte[] bytes, ulong addStart) {

            int copyOffset = (int)((addStart - addressStart) * 2);

            if(bytes.Length + copyOffset <= underlyingBytes.Length) {

                bytes.CopyTo(underlyingBytes, copyOffset);
                UpdateAreaRegisterValues();

            }

        }

        private List<Word> GetUnderlyingWords () {

            var bytes = GetUnderlyingBytes((uint)AddressStart, (int)(addressEnd - AddressStart) + 1);
            var words = new List<Word>();

            for (int i = 0; i < bytes.Length / 2; i += 2) {

                words.Add(new Word(new byte[] { bytes[i], bytes[i + 1] }));

            }

            return words;
        
        }

        private string GetAddressRangeString() {

            switch (registerType) {
                case RegisterPrefix.X:
                case RegisterPrefix.Y:
                case RegisterPrefix.R:
                return $"W{registerType}{AddressStart}-{AddressEnd}";
                case RegisterPrefix.DT:
                case RegisterPrefix.DDT:
                return $"DT{AddressStart}-{AddressEnd}";
            }

            return "";

        }

        public override string ToString() => $"{GetAddressRangeString()} ({managedRegisters.Count} Registers)";

        private protected void OnPropChange([CallerMemberName] string propertyName = null) {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

    }

}
