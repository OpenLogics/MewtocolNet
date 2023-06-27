using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Reflection;

namespace MewtocolNet
{

    internal struct RegisterBuildInfo {

        internal string name;
        internal int memoryAddress;
        internal int memorySizeBytes;    
        internal byte? specialAddress;

        internal RegisterType? registerType;
        internal Type dotnetCastType;
        internal Type collectionType;

        internal BaseRegister Build () {

            RegisterType regType = registerType ?? dotnetCastType.ToRegisterTypeDefault();

            Type registerClassType = dotnetCastType.GetDefaultRegisterHoldingType();

            bool isBytesRegister = !registerClassType.IsGenericType && registerClassType == typeof(BytesRegister);

            if (regType.IsNumericDTDDT() && (dotnetCastType == typeof(bool))) {

                //-------------------------------------------
                //as numeric register with boolean bit target
                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;

                //int _adress, int _reservedByteSize, string _name = null
                var parameters = new object[] { memoryAddress, memorySizeBytes, name };
                var instance = (BaseRegister)Activator.CreateInstance(typeof(BytesRegister), flags, null, parameters, null);

                if (collectionType != null)
                    instance.WithCollectionType(collectionType);

                return instance;

            } else if (regType.IsNumericDTDDT() && !isBytesRegister) {

                //-------------------------------------------
                //as numeric register

                var areaAddr = memoryAddress;

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;

                //int _adress, Type _enumType = null, string _name = null
                var parameters = new object[] { areaAddr, null, name };
                var instance = (BaseRegister)Activator.CreateInstance(registerClassType, flags, null, parameters, null);

                if(collectionType != null)
                    instance.WithCollectionType(collectionType);

                return instance;

            }

            if(isBytesRegister) {

                //-------------------------------------------
                //as byte range register

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;
                //int _adress, int _reservedSize, string _name = null
                var parameters = new object[] { memoryAddress, memorySizeBytes, name };
                var instance = (BaseRegister)Activator.CreateInstance(typeof(BytesRegister), flags, null, parameters, null);

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

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;
                var parameters = new object[] { io, spAddr.Value, areaAddr, name };
                var instance = (BoolRegister)Activator.CreateInstance(typeof(BoolRegister), flags, null, parameters, null);

                if (collectionType != null)
                    ((IRegisterInternal)instance).WithCollectionType(collectionType);

                return instance;

            }

            throw new Exception("Failed to build register");

        }

    }

}
