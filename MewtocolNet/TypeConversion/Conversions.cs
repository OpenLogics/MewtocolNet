using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MewtocolNet.TypeConversion {
    
    internal static class Conversions {

        internal static Dictionary<PlcVarType, RegisterType> dictPlcTypeToRegisterType = new Dictionary<PlcVarType, RegisterType> {

            { PlcVarType.BOOL, RegisterType.R },
            { PlcVarType.INT, RegisterType.DT },
            { PlcVarType.UINT, RegisterType.DT },
            { PlcVarType.DINT, RegisterType.DDT },
            { PlcVarType.UDINT, RegisterType.DDT },
            { PlcVarType.REAL, RegisterType.DDT },
            { PlcVarType.TIME, RegisterType.DDT },
            { PlcVarType.WORD, RegisterType.DT },
            { PlcVarType.DWORD, RegisterType.DDT },
            { PlcVarType.STRING, RegisterType.DT_BYTE_RANGE },

        };

        /// <summary>
        /// All conversions for reading dataf from and to the plc
        /// </summary>
        internal static List<IPlcTypeConverter> items = new List<IPlcTypeConverter> {

            new PlcTypeConversion<bool>(RegisterType.R) {
                HoldingRegisterType = typeof(BoolRegister),
                PlcVarType = PlcVarType.BOOL,
                FromRaw = bytes => {

                    return (bool)(bytes[0] == 1);
                },
                ToRaw = value => {

                    return new byte[] { (byte)(value ? 1 : 0) };

                },
            },
            new PlcTypeConversion<bool>(RegisterType.X) {
                HoldingRegisterType = typeof(BoolRegister),
                PlcVarType = PlcVarType.BOOL,
                FromRaw = bytes => {

                    return bytes[0] == 1;
                },
                ToRaw = value => {

                    return new byte[] { (byte)(value ? 1 : 0) };

                },
            },
            new PlcTypeConversion<bool>(RegisterType.Y) {
                HoldingRegisterType = typeof(BoolRegister),
                PlcVarType = PlcVarType.BOOL,
                FromRaw = bytes => {

                    return bytes[0] == 1;
                },
                ToRaw = value => {

                    return new byte[] { (byte)(value ? 1 : 0) };

                },
            },
            new PlcTypeConversion<short>(RegisterType.DT) {
                HoldingRegisterType = typeof(NumberRegister<short>),
                PlcVarType = PlcVarType.INT,
                FromRaw = bytes => {

                    return BitConverter.ToInt16(bytes, 0);
                },
                ToRaw = value => {

                    return BitConverter.GetBytes(value);

                },
            },
            new PlcTypeConversion<ushort>(RegisterType.DT) {
                HoldingRegisterType = typeof(NumberRegister<ushort>),
                PlcVarType = PlcVarType.UINT,
                FromRaw = bytes => {

                    return BitConverter.ToUInt16(bytes, 0);
                },
                ToRaw = value => {

                    return BitConverter.GetBytes(value);

                },
            },
            new PlcTypeConversion<int>(RegisterType.DDT) {
                HoldingRegisterType = typeof(NumberRegister<int>),
                PlcVarType = PlcVarType.DINT,
                FromRaw = bytes => {

                    return BitConverter.ToInt32(bytes, 0);
                },
                ToRaw = value => {

                    return BitConverter.GetBytes(value);

                },
            },
            new PlcTypeConversion<uint>(RegisterType.DDT) {
                HoldingRegisterType = typeof(NumberRegister<uint>),
                PlcVarType = PlcVarType.UDINT,
                FromRaw = bytes => {

                    return BitConverter.ToUInt32(bytes, 0);
                },
                ToRaw = value => {

                    return BitConverter.GetBytes(value);

                },
            },
            new PlcTypeConversion<float>(RegisterType.DDT) {
                HoldingRegisterType = typeof(NumberRegister<float>),
                PlcVarType = PlcVarType.REAL,
                FromRaw = bytes => {

                    var val = BitConverter.ToUInt32(bytes, 0);
                    byte[] floatVals = BitConverter.GetBytes(val);
                    float finalFloat = BitConverter.ToSingle(floatVals, 0);

                    return finalFloat;

                },
                ToRaw = value => {

                    return BitConverter.GetBytes(value);

                },
            },
            new PlcTypeConversion<TimeSpan>(RegisterType.DDT) {
                HoldingRegisterType = typeof(NumberRegister<TimeSpan>),
                PlcVarType = PlcVarType.TIME,
                FromRaw = bytes => {

                    var vallong = BitConverter.ToUInt32(bytes, 0);
                    var valMillis = vallong * 10;
                    var ts = TimeSpan.FromMilliseconds(valMillis);
                    return ts;

                },
                ToRaw = value => {

                    var tLong = (uint)(value.TotalMilliseconds / 10);
                    return BitConverter.GetBytes(tLong);

                },
            },
            new PlcTypeConversion<byte[]>(RegisterType.DT) {
                HoldingRegisterType = typeof(BytesRegister),
                FromRaw = bytes => bytes,
                ToRaw = value => value,
            },
            new PlcTypeConversion<BitArray>(RegisterType.DT) {
                HoldingRegisterType = typeof(BytesRegister),
                PlcVarType = PlcVarType.WORD,
                FromRaw = bytes => {

                    BitArray bitAr = new BitArray(bytes);
                    return bitAr;

                },
                ToRaw = value => {

                    byte[] ret = new byte[(value.Length - 1) / 8 + 1];
                    value.CopyTo(ret, 0);
                    return ret;

                },
            },

        };

    }

}
