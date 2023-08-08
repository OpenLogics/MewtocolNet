using MewtocolNet.Registers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {

    public abstract class AreaBase {

        private MewtocolInterface mewInterface;

        internal RegisterPrefix registerType;
        internal ulong addressStart;
        internal ulong addressEnd;

        internal byte[] underlyingBytes = new byte[2];

        /// <summary>
        /// List of register link groups that are managed in this memory area
        /// </summary>
        internal List<LinkedRegisterGroup> managedRegisters = new List<LinkedRegisterGroup>();

        public ulong AddressStart => addressStart;
        public ulong AddressEnd => addressEnd;

        internal AreaBase(MewtocolInterface mewIf) {

            mewInterface = mewIf;

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

        }

        public void UpdateAreaRegisterValues() {

            foreach (var register in this.managedRegisters.SelectMany(x => x.Linked)) {

                var regStart = register.MemoryAddress;
                var addLen = (int)register.GetRegisterAddressLen();

                var bytes = this.GetUnderlyingBytes(regStart, addLen);
                register.SetValueFromBytes(bytes);

            }

        }

        internal async Task<bool> RequestByteReadAsync(ulong addStart, ulong addEnd) {

            var byteCount = (addEnd - addStart + 1) * 2;
            var result = await mewInterface.ReadByteRangeNonBlocking((int)addStart, (int)byteCount);

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

        private void SetUnderlyingBytes(byte[] bytes, ulong addStart) {

            int copyOffset = (int)((addStart - addressStart) * 2);

            if(bytes.Length + copyOffset <= underlyingBytes.Length) {

                bytes.CopyTo(underlyingBytes, copyOffset);
                UpdateAreaRegisterValues();

            }

        }

        public override string ToString() => $"DT{AddressStart}-{AddressEnd}";

        public virtual string GetName() => $"{ToString()} ({managedRegisters.Count} Registers)";

        public string GetHash() => GetHashCode().ToString();

    }

}
