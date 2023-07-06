using System;
using System.ComponentModel;

namespace MewtocolNet {

    internal class PlcTypeConversion<T> : IPlcTypeConverter {

        public Type MainType { get; private set; }

        public RegisterType PlcType { get; private set; }

        public PlcVarType PlcVarType { get; set; }

        public Type HoldingRegisterType { get; set; }

        public Func<IRegister, byte[], T> FromRaw { get; set; }

        public Func<IRegister, T, byte[]> ToRaw { get; set; }

        public PlcTypeConversion(RegisterType plcType) {

            MainType = typeof(T);
            PlcType = plcType;

        }

        public Type GetDotnetType() => MainType;

        public Type GetHoldingRegisterType() => HoldingRegisterType;

        public RegisterType GetPlcRegisterType() => PlcType;

        public PlcVarType GetPlcVarType() => PlcVarType;

        public object FromRawData(IRegister register, byte[] data) => FromRaw.Invoke(register, data);

        public byte[] ToRawData(IRegister register, object value) => ToRaw.Invoke(register, (T)value);  


    }

}
