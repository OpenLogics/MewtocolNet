using MewtocolNet.Registers;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {

    internal interface IMemoryArea {

        byte[] GetUnderlyingBytes(BaseRegister reg);

        Task<bool> ReadRegisterAsync(BaseRegister reg);

        Task<bool> WriteRegisterAsync(BaseRegister reg, byte[] bytes);

    }

}
