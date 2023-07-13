using MewtocolNet.Registers;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {

    internal interface IMemoryArea {

        byte[] GetUnderlyingBytes(BaseRegister reg);

        void SetUnderlyingBytes(BaseRegister reg, byte[] bytes);

        void UpdateAreaRegisterValues();

    }

}
