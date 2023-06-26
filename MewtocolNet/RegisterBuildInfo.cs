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

        internal IRegister Build () {

            RegisterType regType = registerType ?? dotnetCastType.ToRegisterTypeDefault();

            PlcVarType plcType = dotnetCastType.ToPlcVarType();
            Type registerClassType = plcType.GetDefaultPlcVarType();

            if (regType.IsNumericDTDDT() && (dotnetCastType == typeof(bool) || dotnetCastType == typeof(BitArray))) {

                //-------------------------------------------
                //as numeric register with boolean bit target

                var type = typeof(NumberRegister<BitArray>);

                var areaAddr = memoryAddress;

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;

                //int _adress, string _name = null, bool isBitwise = false, Type _enumType = null
                var parameters = new object[] { areaAddr, name, true, null };
                var instance = (IRegister)Activator.CreateInstance(type, flags, null, parameters, null);

                if (collectionType != null)
                    ((IRegisterInternal)instance).WithCollectionType(collectionType);

                return instance;

            } else if (regType.IsNumericDTDDT()) {

                //-------------------------------------------
                //as numeric register

                var type = plcType.GetDefaultPlcVarType();

                var areaAddr = memoryAddress;

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;

                //int _adress, string _name = null, bool isBitwise = false, Type _enumType = null
                var parameters = new object[] { areaAddr, name, false, null };
                var instance = (IRegister)Activator.CreateInstance(type, flags, null, parameters, null);

                if(collectionType != null)
                    ((IRegisterInternal)instance).WithCollectionType(collectionType);

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

            if(regType == RegisterType.DT_RANGE) {

                //-------------------------------------------
                //as byte range register

                var type = plcType.GetDefaultPlcVarType();

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;
                //int _adress, int _reservedSize, string _name = null
                var parameters = new object[] { memoryAddress, memorySizeBytes, name };
                var instance = (IRegister)Activator.CreateInstance(type, flags, null, parameters, null);

                if (collectionType != null)
                    ((IRegisterInternal)instance).WithCollectionType(collectionType);

                return instance;

            }

            throw new Exception("Failed to build register");

        }

    }

}
