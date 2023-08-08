using MewtocolNet.Registers;

namespace MewtocolNet.UnderlyingRegisters {

    internal interface IMemoryArea {

        string GetName();

        byte[] GetUnderlyingBytes(Register reg);

        void SetUnderlyingBytes(Register reg, byte[] bytes);

        void UpdateAreaRegisterValues();

    }

}
