using System;
using System.ComponentModel;

namespace MewtocolNet {

    internal class PlcTypeConversion<T> : IPlcTypeConverter {

        public Type MainType { get; private set; }

        public RegisterType PlcType { get; private set; }

        public PlcVarType PlcVarType { get; set; }

        public Type HoldingRegisterType { get; set; }

        public Func<byte[], T> FromRaw { get; set; }

        public Func<T, byte[]> ToRaw { get; set; }

        public PlcTypeConversion(RegisterType plcType) {

            MainType = typeof(T);
            PlcType = plcType;

        }

        public Type GetDotnetType() => MainType;

        public Type GetHoldingRegisterType() => HoldingRegisterType;

        public RegisterType GetPlcRegisterType() => PlcType;

        public PlcVarType GetPlcVarType() => PlcVarType;

        public object FromRawData(byte[] data) => FromRaw.Invoke(data);

        public byte[] ToRawData(object value) => ToRaw.Invoke((T)value);  


    }

}
