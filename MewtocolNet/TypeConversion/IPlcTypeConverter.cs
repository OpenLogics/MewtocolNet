using System;

namespace MewtocolNet {

    internal interface IPlcTypeConverter {

        object FromRawData(IRegister register, byte[] data);

        byte[] ToRawData(IRegister register, object value);

        Type GetDotnetType();

        Type GetHoldingRegisterType();

        RegisterType GetPlcRegisterType();

        PlcVarType GetPlcVarType();

    }

}
