using System;

namespace MewtocolNet {

    internal interface IPlcTypeConverter {

        object FromRawData(byte[] data);

        byte[] ToRawData(object value);

        Type GetDotnetType();

        Type GetHoldingRegisterType();

        RegisterType GetPlcRegisterType();

        PlcVarType GetPlcVarType();

    }

}
