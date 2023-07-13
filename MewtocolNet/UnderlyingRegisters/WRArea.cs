using MewtocolNet.Registers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {

    public class WRArea : IMemoryArea {

        private MewtocolInterface mewInterface;

        internal RegisterType registerType;
        internal ulong addressStart;

        internal byte[] wordData = new byte[2];

        internal List<BaseRegister> linkedRegisters = new List<BaseRegister>();

        public ulong AddressStart => addressStart;

        internal WRArea(MewtocolInterface mewIf) {

            mewInterface = mewIf;

        }

        public void UpdateAreaRegisterValues () {



        }
        public void SetUnderlyingBytes(BaseRegister reg, byte[] bytes) {


        }

        public byte[] GetUnderlyingBytes(BaseRegister reg) {

            return null;

        }

        public async Task<bool> ReadRegisterAsync(BaseRegister reg) {

            return true;

        }

        public async Task<bool> WriteRegisterAsync(BaseRegister reg, byte[] bytes) {

            return true;    
        
        }

        public string GetMewtocolIdent() => GetMewtocolIdentsAllBits();

        public string GetMewtocolIdentsAllBits () {

            StringBuilder asciistring = new StringBuilder();

            for (byte i = 0; i < 16; i++) {

                asciistring.Append(GetMewtocolIdentSingleBit(i));

            }

            return asciistring.ToString();

        }

        public string GetMewtocolIdentSingleBit (byte specialAddress) {

            //(R|X|Y)(area add [3] + special add [1])
            StringBuilder asciistring = new StringBuilder();

            string prefix = registerType.ToString();
            string mem = AddressStart.ToString();
            string sp = specialAddress.ToString("X1");

            asciistring.Append(prefix);
            asciistring.Append(mem.PadLeft(3, '0'));
            asciistring.Append(sp);

            return asciistring.ToString();

        }

        public override string ToString() => $"{registerType}{AddressStart} 0-F";

    }

}
