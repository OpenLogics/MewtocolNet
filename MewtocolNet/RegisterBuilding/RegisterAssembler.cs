using MewtocolNet.Exceptions;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using static MewtocolNet.RegisterBuilding.RBuild;

namespace MewtocolNet.RegisterBuilding {
    internal class RegisterAssembler {

        internal RegisterCollection collectionTarget;

        internal MewtocolInterface onInterface;

        internal RegisterAssembler (MewtocolInterface interf) {

            onInterface = interf;

        }

        internal List<BaseRegister> AssembleAll (RBuild rBuildData) {

            List<BaseRegister> generatedInstances = new List<BaseRegister>();       

            foreach (var data in rBuildData.unfinishedList) {

                var generatedInstance = Assemble(data);

                generatedInstances.Add(generatedInstance);

            }

            return generatedInstances;

        } 

        internal BaseRegister Assemble (SData data) {

            //parse all others where the type is known
            Type registerClassType = data.dotnetVarType.GetDefaultRegisterHoldingType();

            BaseRegister generatedInstance = null;

            if (data.dotnetVarType.IsEnum) {

                //-------------------------------------------
                //as numeric register with enum target

                var underlying = Enum.GetUnderlyingType(data.dotnetVarType);
                var enuSize = Marshal.SizeOf(underlying);

                if (enuSize > 4)
                    throw new NotSupportedException("Enums not based on 16 or 32 bit numbers are not supported");

                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                Type paramedClass = typeof(NumberRegister<>).MakeGenericType(data.dotnetVarType);
                ConstructorInfo constr = paramedClass.GetConstructor(flags, null, new Type[] { typeof(uint), typeof(string) }, null);

                var parameters = new object[] { data.memAddress, data.name };
                var instance = (BaseRegister)constr.Invoke(parameters);

                generatedInstance = instance;

            } else if (registerClassType.IsGenericType) {

                //-------------------------------------------
                //as numeric register

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

                //int _adress, Type _enumType = null, string _name = null
                var parameters = new object[] { data.memAddress, data.name };
                var instance = (BaseRegister)Activator.CreateInstance(registerClassType, flags, null, parameters, null);
                instance.pollLevel = data.pollLevel;
                generatedInstance = instance;

            } else if (registerClassType == typeof(BytesRegister) && data.byteSize != null) {

                //-------------------------------------------
                //as byte range register

                BytesRegister instance = new BytesRegister(data.memAddress, (uint)data.byteSize, data.name);
                generatedInstance = instance;

            } else if (registerClassType == typeof(BytesRegister) && data.bitSize != null) {

                //-------------------------------------------
                //as bit range register

                BytesRegister instance = new BytesRegister(data.memAddress, (ushort)data.bitSize, data.name);
                generatedInstance = instance;

            } else if (registerClassType == typeof(StringRegister)) {

                //-------------------------------------------
                //as byte range register
                var instance = (BaseRegister)new StringRegister(data.memAddress, data.name) {
                    ReservedSize = (short)(data.stringSize ?? 0),
                };
                generatedInstance = instance;

            } else if (data.regType.IsBoolean()) {

                //-------------------------------------------
                //as boolean register

                var io = (IOType)(int)data.regType;
                var spAddr = data.specialAddress;
                var areaAddr = data.memAddress;

                var instance = new BoolRegister(io, spAddr, areaAddr, data.name);

                generatedInstance = instance;

            }

            //finalize set for every

            if (generatedInstance == null)
                throw new MewtocolException("Failed to build register");

            if (collectionTarget != null)
                generatedInstance.WithRegisterCollection(collectionTarget);

            generatedInstance.attachedInterface = onInterface;

            generatedInstance.pollLevel = data.pollLevel;

            return generatedInstance;   

        }

    }

}
