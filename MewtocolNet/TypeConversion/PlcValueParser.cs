using MewtocolNet.Exceptions;
using MewtocolNet.Registers;
using MewtocolNet.TypeConversion;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MewtocolNet {

    internal static class PlcValueParser {

        private static List<IPlcTypeConverter> conversions => Conversions.items;

        public static T Parse<T>(IRegister register, byte[] bytes) {

            var converter = conversions.FirstOrDefault(x => x.GetDotnetType() == typeof(T));

            if (converter == null)
                throw new MewtocolException($"A converter for the dotnet type {typeof(T)} doesn't exist");

            return (T)converter.FromRawData(register, bytes);

        }

        public static byte[] Encode <T>(IRegister register, T value) {

            var converter = conversions.FirstOrDefault(x => x.GetDotnetType() == typeof(T));

            if (converter == null)
                throw new MewtocolException($"A converter for the dotnet type {typeof(T)} doesn't exist");

            return converter.ToRawData(register, value);

        }

        public static List<Type> GetAllowDotnetTypes () => conversions.Select(x => x.GetDotnetType()).ToList();

        public static List<Type> GetAllowRegisterTypes () => conversions.Select(x => x.GetHoldingRegisterType()).ToList();

        public static RegisterType? GetDefaultRegisterType (Type type) => 
            conversions.FirstOrDefault(x => x.GetDotnetType() == type)?.GetPlcRegisterType();

        public static Type GetDefaultRegisterHoldingType (this PlcVarType type) =>
            conversions.FirstOrDefault(x => x.GetPlcVarType() == type)?.GetHoldingRegisterType();

        public static Type GetDefaultRegisterHoldingType (this Type type) =>
            conversions.FirstOrDefault(x => x.GetDotnetType() == type)?.GetHoldingRegisterType();

        public static Type GetDefaultDotnetType (this PlcVarType type) =>
            conversions.FirstOrDefault(x => x.GetPlcVarType() == type)?.GetDotnetType();

        public static PlcVarType? GetDefaultPlcVarType (this Type type) =>
            conversions.FirstOrDefault(x => x.GetDotnetType() == type)?.GetPlcVarType();

    }

}
