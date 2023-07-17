using MewtocolNet.Registers;
using MewtocolNet.TypeConversion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MewtocolNet {

    internal static class PlcValueParser {

        private static List<IPlcTypeConverter> conversions => Conversions.items;

        internal static T Parse<T>(Register register, byte[] bytes) {

            IPlcTypeConverter converter;
            Type underlyingType;

            //special case for enums
            if (typeof(T).IsEnum) {

                underlyingType = typeof(T).GetEnumUnderlyingType();

            } else {

                underlyingType = typeof(T);

            }

            converter = conversions.FirstOrDefault(x => x.GetDotnetType() == underlyingType);

            if (converter == null)
                throw new Exception($"A converter for the dotnet type {underlyingType} doesn't exist");

            return (T)converter.FromRawData(register, bytes);

        }

        internal static T ParseArray <T>(Register register, int[] indices, byte[] bytes) {

            IPlcTypeConverter converter;
            Type underlyingElementType;

            //special case for enums
            if (typeof(T).IsEnum) {

                underlyingElementType = typeof(T).GetElementType().GetEnumUnderlyingType();

            } else {

                underlyingElementType = typeof(T).GetElementType();

            }

            converter = conversions.FirstOrDefault(x => x.GetDotnetType() == underlyingElementType);

            if (converter == null)
                throw new Exception($"A converter for the dotnet type {underlyingElementType} doesn't exist");

            //parse the array from one to n dimensions
            var outArray = Array.CreateInstance(underlyingElementType, indices);

            if(outArray.GetType() == typeof(byte[])) {

                Console.WriteLine();
            
            }

            int sizePerItem = underlyingElementType.DetermineTypeByteIntialSize();

            var iterateItems = indices.Aggregate((a, x) => a * x);
            var indexer = new int[indices.Length];
            for (int i = 0; i < iterateItems; i++) {

                int j = i * sizePerItem;    

                var currentItem = bytes.Skip(j).Take(sizePerItem).ToArray();
                var value = converter.FromRawData(register, currentItem);

                for (int remainder = i, k = indices.Length - 1; k >= 0; k--) {

                    int currentDimension = indices[k];
                    indexer[k] = remainder % currentDimension;
                    remainder = remainder / currentDimension;
                
                }

                outArray.SetValue(value, indexer);

            }

            return (T)(object)outArray;

        }

        static void ConvertFlatArrayToDim (
            IPlcTypeConverter converter,
            Register register,
            byte[] source, 
            Array target, 
            int sizePerVal, 
            int[] dims, 
            int currentIndex, 
            int currentArrayIndex
        ) {
            
            if (currentIndex == dims.Length - 1) {

                for (int i = 0; i < dims[currentIndex]; i++) {

                    byte[] rawDataItem = source.Skip(currentArrayIndex).Take(sizePerVal).ToArray();
                    var value = converter.FromRawData(register, rawDataItem);
                
                    target.SetValue(value, i);
                    currentArrayIndex += sizePerVal;
                
                }

            } else {

                for (int i = 0; i < dims[currentIndex]; i++) {
                
                    Array innerArray = (Array)target.GetValue(i);
                    ConvertFlatArrayToDim(converter, register, source, innerArray, sizePerVal, dims, currentIndex + 1, currentArrayIndex);
                    currentArrayIndex += innerArray.Length * sizePerVal;
               
                }

            }

        }


        internal static byte[] Encode<T>(Register register, T value) {

            IPlcTypeConverter converter;
            Type underlyingType;

            //special case for enums
            if (typeof(T).IsEnum) {

                underlyingType = typeof(T).GetEnumUnderlyingType();

            } else {

                underlyingType = typeof(T); 

            }

            converter = conversions.FirstOrDefault(x => x.GetDotnetType() == underlyingType);

            if (converter == null)
                throw new Exception($"A converter for the type {underlyingType} doesn't exist");

            return converter.ToRawData(register, value);

        }

        //internal static byte[] EncodeArray (IRegister register, T value) {


        //}

        public static List<Type> GetAllowDotnetTypes() => conversions.Select(x => x.GetDotnetType()).ToList();

        public static List<Type> GetAllowRegisterTypes() => conversions.Select(x => x.GetHoldingRegisterType()).ToList();

        public static RegisterType? GetDefaultRegisterType(Type type) =>
            conversions.FirstOrDefault(x => x.GetDotnetType() == type)?.GetPlcRegisterType();

        public static Type GetDefaultRegisterHoldingType(this PlcVarType type) =>
            conversions.FirstOrDefault(x => x.GetPlcVarType() == type)?.GetHoldingRegisterType();

        public static Type GetDefaultRegisterHoldingType(this Type type) =>
            conversions.FirstOrDefault(x => x.GetDotnetType() == type)?.GetHoldingRegisterType();

        public static Type GetDefaultDotnetType(this PlcVarType type) =>
            conversions.FirstOrDefault(x => x.GetPlcVarType() == type)?.GetDotnetType();

        public static PlcVarType? GetDefaultPlcVarType(this Type type) =>
            conversions.FirstOrDefault(x => x.GetDotnetType() == type)?.GetPlcVarType();

    }

}
