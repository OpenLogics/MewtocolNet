using MewtocolNet.Exceptions;
using MewtocolNet.RegisterAttributes;
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

        internal RegisterCollection collectionTarget;
        internal PropertyInfo boundPropTarget;

        internal BaseRegister BuildForCollectionAttribute () {

            return (BaseRegister)RegBuilder.Factory.FromPlcRegName(mewAddress, name).AsType(dotnetCastType).Build();

        }

        internal BaseRegister Build () {

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

                if (collectionTarget != null)
                    instance.WithRegisterCollection(collectionTarget);

                return instance;

            }

            //parse all others where the type is known
            RegisterType regType = registerType ?? dotnetCastType.ToRegisterTypeDefault();
            Type registerClassType = dotnetCastType.GetDefaultRegisterHoldingType();

            bool isBoolRegister = regType.IsBoolean();

            bool isBytesArrRegister = !registerClassType.IsGenericType && registerClassType == typeof(BytesRegister) && dotnetCastType == typeof(byte[]);

            bool isBytesBitsRegister = !registerClassType.IsGenericType && registerClassType == typeof(BytesRegister) && dotnetCastType == typeof(BitArray);

            bool isStringRegister = !registerClassType.IsGenericType && registerClassType == typeof(StringRegister);

            bool isNormalNumericResiter = regType.IsNumericDTDDT() && !isBytesArrRegister && !isBytesBitsRegister && !isStringRegister;

            if (isNormalNumericResiter) {

                //-------------------------------------------
                //as numeric register

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;

                //int _adress, Type _enumType = null, string _name = null
                var parameters = new object[] { memoryAddress, name };
                var instance = (BaseRegister)Activator.CreateInstance(registerClassType, flags, null, parameters, null);

                if(collectionTarget != null)
                    instance.WithRegisterCollection(collectionTarget);

                return instance;

            }

            if(isBytesArrRegister) {

                //-------------------------------------------
                //as byte range register

                BytesRegister instance = new BytesRegister(memoryAddress, memorySizeBytes.Value, name);
                instance.ReservedBytesSize = (ushort)memorySizeBytes.Value; 

                if (collectionTarget != null)
                    instance.WithRegisterCollection(collectionTarget);

                return instance;

            }

            if(isBytesBitsRegister) {

                //-------------------------------------------
                //as bit range register

                BytesRegister instance;

                if (memorySizeBits != null) {
                    instance = new BytesRegister(memoryAddress, memorySizeBits.Value, name);
                } else {
                    instance = new BytesRegister(memoryAddress, 16, name);
                }

                if (collectionTarget != null)
                    instance.WithRegisterCollection(collectionTarget);

                return instance;

            }

            if (isStringRegister) {

                //-------------------------------------------
                //as byte range register
                var instance = (BaseRegister)new StringRegister(memoryAddress, name);   

                if (collectionTarget != null)
                    instance.WithRegisterCollection(collectionTarget);

                return instance;

            }

            if (isBoolRegister) {

                //-------------------------------------------
                //as boolean register

                var io = (IOType)(int)regType;
                var spAddr = specialAddress;
                var areaAddr = memoryAddress;

                var instance = new BoolRegister(io, spAddr.Value, areaAddr, name);

                if (collectionTarget != null)
                    instance.WithRegisterCollection(collectionTarget);

                return instance;

            }

            throw new Exception("Failed to build register");

        }

    }

}
