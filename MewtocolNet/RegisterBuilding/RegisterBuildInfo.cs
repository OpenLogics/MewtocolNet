using MewtocolNet.Exceptions;
using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Data.Common;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MewtocolNet.RegisterBuilding {

    internal struct RegisterBuildInfo {

        internal string mewAddress;

        internal string name;
        internal uint memoryAddress;

        internal uint? memorySizeBytes;    
        internal ushort? memorySizeBits;
        internal byte? specialAddress;

        internal RegisterType? registerType;
        internal Type dotnetCastType;
        internal Type collectionType;

        internal BaseRegister Build () {

            //Has mew address use this before the default checks
            if (mewAddress != null) return BuildFromMewAddress();

            //parse enums 
            if (dotnetCastType.IsEnum) {

                //-------------------------------------------
                //as numeric register with enum target

                var underlying = Enum.GetUnderlyingType(dotnetCastType);
                var enuSize = Marshal.SizeOf(underlying);

                if (enuSize > 4)
                    throw new NotSupportedException("Enums not based on 16 or 32 bit numbers are not supported");

                Type myParameterizedSomeClass = typeof(NumberRegister<>).MakeGenericType(dotnetCastType);
                ConstructorInfo constr = myParameterizedSomeClass.GetConstructor(new Type[] { typeof(uint), typeof(string) });

                var parameters = new object[] { memoryAddress, name };
                var instance = (BaseRegister)constr.Invoke(parameters);

                if (collectionType != null)
                    instance.WithCollectionType(collectionType);

                return instance;

            }

            //parse all others where the type is known
            RegisterType regType = registerType ?? dotnetCastType.ToRegisterTypeDefault();
            Type registerClassType = dotnetCastType.GetDefaultRegisterHoldingType();
            bool isBytesRegister = !registerClassType.IsGenericType && registerClassType == typeof(BytesRegister);
            bool isStringRegister = !registerClassType.IsGenericType && registerClassType == typeof(StringRegister);

            if (regType.IsNumericDTDDT() && (dotnetCastType == typeof(bool))) {

                //-------------------------------------------
                //as numeric register with boolean bit target
                //create a new bregister instance
                var instance = new BytesRegister(memoryAddress, memorySizeBytes.Value, name);

                if (collectionType != null)
                    instance.WithCollectionType(collectionType);

                return instance;

            } else if (regType.IsNumericDTDDT() && !isBytesRegister && !isStringRegister) {

                //-------------------------------------------
                //as numeric register

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;

                //int _adress, Type _enumType = null, string _name = null
                var parameters = new object[] { memoryAddress, name };
                var instance = (BaseRegister)Activator.CreateInstance(registerClassType, flags, null, parameters, null);

                if(collectionType != null)
                    instance.WithCollectionType(collectionType);

                return instance;

            }

            if(isBytesRegister) {

                //-------------------------------------------
                //as byte range register

                BytesRegister instance;

                if(memorySizeBits != null) {
                    instance = new BytesRegister(memoryAddress, memorySizeBits.Value, name);
                } else {
                    instance = new BytesRegister(memoryAddress, memorySizeBytes.Value, name);
                }

                if (collectionType != null)
                    instance.WithCollectionType(collectionType);

                return instance;

            }

            if (isStringRegister) {

                //-------------------------------------------
                //as byte range register
                var instance = (BaseRegister)new StringRegister(memoryAddress, name);   

                if (collectionType != null)
                    instance.WithCollectionType(collectionType);

                return instance;

            }

            if (regType.IsBoolean()) {

                //-------------------------------------------
                //as boolean register

                var io = (IOType)(int)regType;
                var spAddr = specialAddress;
                var areaAddr = memoryAddress;

                var instance = new BoolRegister(io, spAddr.Value, areaAddr, name);

                if (collectionType != null)
                    ((IRegisterInternal)instance).WithCollectionType(collectionType);

                return instance;

            }

            throw new Exception("Failed to build register");

        }

        private BaseRegister BuildFromMewAddress () {

            return (BaseRegister)RegBuilder.Factory.FromPlcRegName(mewAddress, name).AsType(dotnetCastType).Build();

        }

    }

}
