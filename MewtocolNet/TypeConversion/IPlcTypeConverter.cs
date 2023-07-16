using MewtocolNet.Registers;
using System;

namespace MewtocolNet {

    internal interface IPlcTypeConverter {

        object FromRawData(Register register, byte[] data);

        byte[] ToRawData(Register register, object value);

        Type GetDotnetType();

        Type GetHoldingRegisterType();

        RegisterType GetPlcRegisterType();

        PlcVarType GetPlcVarType();

    }

}
