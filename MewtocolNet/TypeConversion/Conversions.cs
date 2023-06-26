using MewtocolNet.Registers;
using System;
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
            { PlcVarType.STRING, RegisterType.DT_RANGE },

        };

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

        };

    }

}
