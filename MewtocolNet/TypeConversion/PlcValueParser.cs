using MewtocolNet.Registers;
using MewtocolNet.TypeConversion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                throw new Exception($"A converter for the type {underlyingType} doesn't exist");

            return (T)converter.FromRawData(register, bytes);

        }

        internal static object ParseArray <T>(ArrayRegister<T> register, byte[] bytes) {

            //if byte array directly return the bytes
            if (typeof(T) == typeof(byte[])) return bytes;

            IPlcTypeConverter converter;
            Type underlyingElementType;

            //special case for enums
            if (typeof(T).IsEnum) {

                underlyingElementType = typeof(T).GetEnumUnderlyingType();

            } else {

                underlyingElementType = typeof(T);

            }

            converter = conversions.FirstOrDefault(x => x.GetDotnetType() == underlyingElementType);

            if (converter == null)
                throw new Exception($"A converter for the type {underlyingElementType} doesn't exist");

            //parse the array from one to n dimensions
            var outArray = Array.CreateInstance(typeof(T), register.indices);

            int sizePerItem = 0;

            if(underlyingElementType == typeof(string)) {
                sizePerItem = register.byteSizePerItem;
            } else {
                sizePerItem = underlyingElementType.DetermineTypeByteIntialSize();
            }

            var iterateItems = register.indices.Aggregate((a, x) => a * x);
            var indexer = new int[register.indices.Length];
            for (int i = 0; i < iterateItems; i++) {

                int j = i * sizePerItem;    

                var currentItem = bytes.Skip(j).Take(sizePerItem).ToArray();
                var value = converter.FromRawData(register, currentItem);

                for (int remainder = i, k = register.indices.Length - 1; k >= 0; k--) {

                    int currentDimension = register.indices[k];
                    indexer[k] = remainder % currentDimension;
                    remainder = remainder / currentDimension;
                
                }

                outArray.SetValue(value, indexer);

            }

            return (object)outArray;

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

        internal static byte[] EncodeArray<T>(ArrayRegister<T> register, object value) {

            //if byte array directly return the bytes
            if (value.GetType() == typeof(byte[])) return (byte[])value;

            IPlcTypeConverter converter;
            Type underlyingElementType;

            //special case for enums
            if (typeof(T).IsEnum) {

                underlyingElementType = typeof(T).GetEnumUnderlyingType();

            } else {

                underlyingElementType = typeof(T);

            }

            converter = conversions.FirstOrDefault(x => x.GetDotnetType() == underlyingElementType);

            if (converter == null)
                throw new Exception($"A converter for the type {underlyingElementType} doesn't exist");

            int sizePerItem = 0;

            if (underlyingElementType == typeof(string)) {
                sizePerItem = register.byteSizePerItem;
            } else {
                sizePerItem = underlyingElementType.DetermineTypeByteIntialSize();
            }

            byte[] encodedData = new byte[((ICollection)value).Count * sizePerItem];

            int i = 0;
            foreach (object item in (IEnumerable)value) {

                var encoded = converter.ToRawData(register, item);

                if(encoded.Length > register.byteSizePerItem)
                    throw new ArgumentOutOfRangeException(nameof(value), "Input mismatched register target size");

                encoded.CopyTo(encodedData, i);

                i += sizePerItem;

            }


            if (encodedData.Length != register.reservedByteSize)
                throw new ArgumentOutOfRangeException(nameof(value), "Input mismatched register target size");

            return encodedData;

        }

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
