using MewtocolNet.Registers;

namespace MewtocolNet.UnderlyingRegisters {

    internal interface IMemoryArea {

        byte[] GetUnderlyingBytes(Register reg);

        void SetUnderlyingBytes(Register reg, byte[] bytes);

        void UpdateAreaRegisterValues();

    }

}
