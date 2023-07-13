using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MewtocolNet.Helpers;
using MewtocolNet.Exceptions;

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
        /// All conversions for reading data from and to the plc, excluding Enum types
        /// </summary>
        internal static List<IPlcTypeConverter> items = new List<IPlcTypeConverter> {

            //default bool R conversion
            new PlcTypeConversion<bool>(RegisterType.R) {
                HoldingRegisterType = typeof(BoolRegister),
                PlcVarType = PlcVarType.BOOL,
                FromRaw = (reg, bytes) => (bool)(bytes[0] == 1),
                ToRaw = (reg, value) => new byte[] { (byte)(value ? 1 : 0) },
            },

            //default bool X conversion
            new PlcTypeConversion<bool>(RegisterType.X) {
                HoldingRegisterType = typeof(BoolRegister),
                PlcVarType = PlcVarType.BOOL,
                FromRaw = (reg, bytes) => (bool)(bytes[0] == 1),
                ToRaw = (reg, value) => new byte[] { (byte)(value ? 1 : 0) },
            },

            //default bool Y conversion
            new PlcTypeConversion<bool>(RegisterType.Y) {
                HoldingRegisterType = typeof(BoolRegister),
                PlcVarType = PlcVarType.BOOL,
                FromRaw = (reg, bytes) => (bool)(bytes[0] == 1),
                ToRaw = (reg, value) => new byte[] { (byte)(value ? 1 : 0) },
            },

            //default short DT conversion
            new PlcTypeConversion<short>(RegisterType.DT) {
                HoldingRegisterType = typeof(NumberRegister<short>),
                PlcVarType = PlcVarType.INT,
                FromRaw = (reg, bytes) => BitConverter.ToInt16(bytes, 0),
                ToRaw = (reg, value) => BitConverter.GetBytes(value),
            },

            //default ushort DT conversion
            new PlcTypeConversion<ushort>(RegisterType.DT) {
                HoldingRegisterType = typeof(NumberRegister<ushort>),
                PlcVarType = PlcVarType.UINT,
                FromRaw = (reg, bytes) => BitConverter.ToUInt16(bytes, 0),
                ToRaw = (reg, value) => BitConverter.GetBytes(value),
            },

            //default Word DT conversion
            new PlcTypeConversion<Word>(RegisterType.DT) {
                HoldingRegisterType = typeof(NumberRegister<Word>),
                PlcVarType = PlcVarType.WORD,
                FromRaw = (reg, bytes) => new Word(bytes),
                ToRaw = (reg, value) => value.ToByteArray(),
            },

            //default int DDT conversion
            new PlcTypeConversion<int>(RegisterType.DDT) {
                HoldingRegisterType = typeof(NumberRegister<int>),
                PlcVarType = PlcVarType.DINT,
                FromRaw = (reg, bytes) => BitConverter.ToInt32(bytes, 0),
                ToRaw = (reg, value) => BitConverter.GetBytes(value),
            },

            //default uint DDT conversion
            new PlcTypeConversion<uint>(RegisterType.DDT) {
                HoldingRegisterType = typeof(NumberRegister<uint>),
                PlcVarType = PlcVarType.UDINT,
                FromRaw = (reg, bytes) => BitConverter.ToUInt32(bytes, 0),
                ToRaw = (reg, value) => BitConverter.GetBytes(value),
            },

            //default DWord DDT conversion
            new PlcTypeConversion<DWord>(RegisterType.DDT) {
                HoldingRegisterType = typeof(NumberRegister<DWord>),
                PlcVarType = PlcVarType.DWORD,
                FromRaw = (reg, bytes) => new DWord(bytes),
                ToRaw = (reg, value) => value.ToByteArray(),
            },

            //default float DDT conversion
            new PlcTypeConversion<float>(RegisterType.DDT) {
                HoldingRegisterType = typeof(NumberRegister<float>),
                PlcVarType = PlcVarType.REAL,
                FromRaw = (reg, bytes) => BitConverter.ToSingle(bytes, 0),
                ToRaw = (reg, value) => BitConverter.GetBytes(value),
            },

            //default TimeSpan DDT conversion
            new PlcTypeConversion<TimeSpan>(RegisterType.DDT) {
                HoldingRegisterType = typeof(NumberRegister<TimeSpan>),
                PlcVarType = PlcVarType.TIME,
                FromRaw = (reg, bytes) => {

                    var vallong = BitConverter.ToUInt32(bytes, 0);
                    var valMillis = vallong * 10;
                    var ts = TimeSpan.FromMilliseconds(valMillis);
                    return ts;

                },
                ToRaw = (reg, value) => {

                    var tLong = (uint)(value.TotalMilliseconds / 10);
                    return BitConverter.GetBytes(tLong);

                },
            },

            //default byte array DT Range conversion, direct pass through
            new PlcTypeConversion<byte[]>(RegisterType.DT_BYTE_RANGE) {
                HoldingRegisterType = typeof(ArrayRegister),
                FromRaw = (reg, bytes) => bytes,
                ToRaw = (reg, value) => value,
            },

            //default string DT Range conversion Example bytes: (04 00 03 00 XX XX XX) 
            //first 4 bytes are reserved size (2 bytes) and used size (2 bytes)
            //the remaining bytes are the ascii bytes for the string
            new PlcTypeConversion<string>(RegisterType.DT_BYTE_RANGE) {
                HoldingRegisterType = typeof(StringRegister),
                PlcVarType = PlcVarType.STRING,
                FromRaw = (reg, bytes) => {

                    if(bytes == null || bytes.Length <= 4) {

                        throw new MewtocolException("Failed to convert string bytes, response not long enough");

                    }

                    //get actual showed size
                    short actualLen = BitConverter.ToInt16(bytes, 2);

                    //skip 4 bytes because they only describe the length
                    return Encoding.UTF8.GetString(bytes.Skip(4).Take(actualLen).ToArray());
                
                },
                ToRaw = (reg, value) => {

                    var sReg = (StringRegister)reg;

                    if(value.Length > sReg.ReservedSize)
                        value = value.Substring(0, sReg.ReservedSize);

                    int padLen = sReg.ReservedSize;
                    if(sReg.ReservedSize % 2 != 0) padLen++;

                    var strBytes = Encoding.UTF8.GetBytes(value.PadRight(padLen, '\0'));

                    List<byte> finalBytes = new List<byte>();   
                    finalBytes.AddRange(BitConverter.GetBytes(sReg.ReservedSize));
                    finalBytes.AddRange(BitConverter.GetBytes((short)value.Length));
                    finalBytes.AddRange(strBytes);

                    return finalBytes.ToArray();

                },
            },

        };

    }

}
