using MewtocolNet.Registers;
using System;

namespace MewtocolNet {

    internal class PlcTypeConversion<T> : IPlcTypeConverter {

        public Type MainType { get; private set; }

        public RegisterPrefix PlcType { get; private set; }

        public PlcVarType PlcVarType { get; set; }

        public Type HoldingRegisterType { get; set; }

        public Func<Register, byte[], T> FromRaw { get; set; }

        public Func<Register, T, byte[]> ToRaw { get; set; }

        public PlcTypeConversion(RegisterPrefix plcType) {

            MainType = typeof(T);
            PlcType = plcType;

        }

        public Type GetDotnetType() => MainType;

        public Type GetHoldingRegisterType() => HoldingRegisterType;

        public RegisterPrefix GetPlcRegisterType() => PlcType;

        public PlcVarType GetPlcVarType() => PlcVarType;

        public object FromRawData(Register register, byte[] data) => FromRaw.Invoke(register, data);

        public byte[] ToRawData(Register register, object value) => ToRaw.Invoke(register, (T)value);


    }

}
