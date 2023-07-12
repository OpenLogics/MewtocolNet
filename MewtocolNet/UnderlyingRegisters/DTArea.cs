using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {
    public class DTArea : IMemoryArea {

        private MewtocolInterface mewInterface;

        internal RegisterType registerType;
        internal ulong addressStart;
        internal ulong addressEnd;

        internal byte[] underlyingBytes = new byte[2];

        internal List<BaseRegister> linkedRegisters = new List<BaseRegister>(); 

        public ulong AddressStart => addressStart;
        public ulong AddressEnd => addressEnd;

        internal DTArea (MewtocolInterface mewIf) {

            mewInterface = mewIf;
        
        }

        internal void BoundaryUdpdate (uint? addrFrom = null, uint? addrTo = null) {

            var addFrom = addrFrom ?? addressStart;
            var addTo = addrTo ?? addressEnd;

            var oldFrom = addressStart;
            var oldUnderlying = underlyingBytes.ToArray();    

            underlyingBytes = new byte[(addTo + 1 - addFrom) * 2];

            //copy old bytes to new array
            var offset = (int)(oldFrom - addFrom) * 2;
            oldUnderlying.CopyTo(oldUnderlying, offset);

            addressStart = addFrom;
            addressEnd = addTo;

        }

        public void UpdateAreaRegisterValues() {

            foreach (var register in this.linkedRegisters) {

                var regStart = register.MemoryAddress;
                var addLen = (int)register.GetRegisterAddressLen();

                var bytes = this.GetUnderlyingBytes(regStart, addLen);
                register.SetValueFromBytes(bytes);

            }

        }

        public async Task<bool> ReadRegisterAsync (BaseRegister reg) {

            return await RequestByteReadAsync(reg.MemoryAddress, reg.MemoryAddress + reg.GetRegisterAddressLen() - 1);

        }

        public async Task<bool> WriteRegisterAsync (BaseRegister reg, byte[] bytes) {

            return await RequestByteWriteAsync(reg.MemoryAddress, bytes);

        }

        internal async Task<bool> RequestByteReadAsync (ulong addStart, ulong addEnd) {

            await CheckDynamicallySizedRegistersAsync();

            var station = mewInterface.GetStationNumber();

            string requeststring = $"%{station}#RD{GetMewtocolIdent(addStart, addEnd)}";
            var result = await mewInterface.SendCommandAsync(requeststring);

            if(result.Success) {

                var resBytes = result.Response.ParseDTRawStringAsBytes();
                SetUnderlyingBytes(resBytes, addStart);

            }

            return result.Success;

        }

        internal async Task<bool> RequestByteWriteAsync(ulong addStart, byte[] bytes) {

            var station = mewInterface.GetStationNumber();
            var addEnd = addStart + ((ulong)bytes.Length / 2) - 1;

            string requeststring = $"%{station}#WD{GetMewtocolIdent(addStart, addEnd)}{bytes.ToHexString()}";
            var result = await mewInterface.SendCommandAsync(requeststring);

            if (result.Success) {

                SetUnderlyingBytes(bytes, addStart);

            }

            return result.Success;

        }

        public byte[] GetUnderlyingBytes(BaseRegister reg) {

            int byteLen = (int)(reg.GetRegisterAddressLen() * 2);

            return GetUnderlyingBytes(reg.MemoryAddress, byteLen);

        }

        internal byte[] GetUnderlyingBytes (uint addStart, int addLen) {

            int byteLen = (int)(addLen * 2);

            int copyOffset = (int)((addStart - addressStart) * 2);
            var gotBytes = underlyingBytes.Skip(copyOffset).Take(byteLen).ToArray();

            return gotBytes;

        }

        public void SetUnderlyingBytes(BaseRegister reg, byte[] bytes) {

            SetUnderlyingBytes(bytes, reg.MemoryAddress);

        }

        private void SetUnderlyingBytes(byte[] bytes, ulong addStart) {

            int copyOffset = (int)((addStart - addressStart) * 2);
            bytes.CopyTo(underlyingBytes, copyOffset);

            UpdateAreaRegisterValues();

        }

        private async Task CheckDynamicallySizedRegistersAsync () {

            //calibrating at runtime sized registers
            var uncalibratedStringRegisters = linkedRegisters
            .Where(x => x is StringRegister sreg && !sreg.isCalibratedFromPlc)
            .Cast<StringRegister>()
            .ToList();

            foreach (var register in uncalibratedStringRegisters)
                await register.CalibrateFromPLC();

            if (uncalibratedStringRegisters.Count > 0)
                mewInterface.memoryManager.MergeAndSizeDataAreas();

        }

        private string GetMewtocolIdent () {

            StringBuilder asciistring = new StringBuilder("D");
            asciistring.Append(AddressStart.ToString().PadLeft(5, '0'));
            asciistring.Append(AddressEnd.ToString().PadLeft(5, '0'));
            return asciistring.ToString();

        }

        private string GetMewtocolIdent(ulong addStart, ulong addEnd) {

            StringBuilder asciistring = new StringBuilder("D");
            asciistring.Append(addStart.ToString().PadLeft(5, '0'));
            asciistring.Append(addEnd.ToString().PadLeft(5, '0'));
            return asciistring.ToString();

        }

        public override string ToString() => $"DT{AddressStart}-{AddressEnd}";

    }

}
