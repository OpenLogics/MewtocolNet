using MewtocolNet.RegisterAttributes;
using MewtocolNet.RegisterBuilding.BuilderPatterns;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MewtocolNet.RegisterBuilding {

    internal class RegisterAssembler {

        internal RegisterCollection collectionTarget;

        internal MewtocolInterface onInterface;

        internal List<Register> assembled = new List<Register>();

        internal RegisterAssembler(MewtocolInterface interf) {

            onInterface = interf;

        }

        internal Register Assemble(StepData data) {

            Register generatedInstance = null;

            if (data.dotnetVarType.IsArray) {

                //-------------------------------------------
                //as array register

                Type elementType = data.dotnetVarType.GetElementType();

                uint numericSizePerElement = (uint)elementType.DetermineTypeByteIntialSize();

                if (elementType.IsEnum && numericSizePerElement > 4) {
                    if (data.boundProperty != null) {
                        throw new NotSupportedException($"Enums not based on 16 or 32 bit numbers are not supported ({data.boundProperty})");
                    } else {
                        throw new NotSupportedException($"Enums not based on 16 or 32 bit numbers are not supported");
                    }
                }

                var parameters = new object[] { 
                    data.memAddress, 
                    data.byteSizeHint,
                    data.arrayIndicies, 
                    data.name 
                };

                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                
                Type paramedClass = typeof(ArrayRegister<>).MakeGenericType(data.dotnetVarType.GetElementType());
                ConstructorInfo constr = paramedClass.GetConstructor(flags, null, new Type[] {
                    typeof(uint), 
                    typeof(uint), 
                    typeof(int[]), 
                    typeof(string)
                }, null);

                var instance = (Register)constr.Invoke(parameters);

                instance.RegisterType = RegisterType.DT_BYTE_RANGE;

                if (data.boundProperty != null && data.boundProperty.PropertyType != data.dotnetVarType)
                    throw new TypeAccessException($"The bound property {data.boundProperty} must by of type: {data.dotnetVarType}");

                generatedInstance = instance;

            } else if (!data.regType.IsBoolean() && data.dotnetVarType.IsAllowedPlcCastingType() && data.dotnetVarType != typeof(string)) {

                //-------------------------------------------
                //as struct register

                uint numericSize = (uint)data.dotnetVarType.DetermineTypeByteIntialSize();

                if (data.dotnetVarType.IsEnum && numericSize > 4) {
                    if (data.boundProperty != null) {
                        throw new NotSupportedException($"Enums not based on 16 or 32 bit numbers are not supported ({data.boundProperty})");
                    } else {
                        throw new NotSupportedException($"Enums not based on 16 or 32 bit numbers are not supported");
                    }
                }
                
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

                Type paramedClass = typeof(StructRegister<>).MakeGenericType(data.dotnetVarType);
                ConstructorInfo constr = paramedClass.GetConstructor(flags, null, new Type[] {
                    typeof(uint), typeof(uint) ,typeof(string)
                }, null);

                var parameters = new object[] { 
                    data.memAddress, 
                    numericSize, 
                    data.name 
                };

                var instance = (Register)constr.Invoke(parameters);

                generatedInstance = instance;

            } else if (!data.regType.IsBoolean() && data.dotnetVarType.IsAllowedPlcCastingType()) {

                //-------------------------------------------
                //as string register

                uint numericSize = 0;

                if (data.dotnetVarType == typeof(string)) {

                    if (data.byteSizeHint == null)
                        throw new NotSupportedException($"Can't create a STRING register without a string size hint");

                    if (data.byteSizeHint < 0)
                        throw new NotSupportedException($"Can't create a STRING register with a string size hint < 0");

                    numericSize = data.byteSizeHint.Value;

                }

                var instance = (Register)new StringRegister(data.memAddress, numericSize, data.name);

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
                throw new ArgumentException("Failed to build register");

            if (collectionTarget != null)
                generatedInstance.WithRegisterCollection(collectionTarget);

            if (data.boundProperty != null)
                generatedInstance.WithBoundProperty(new RegisterPropTarget {
                    BoundProperty = data.boundProperty,
                });

            generatedInstance.attachedInterface = onInterface;
            generatedInstance.underlyingSystemType = data.dotnetVarType;
            generatedInstance.pollLevel = data.pollLevel;

            if (data.regCollection != null)
                generatedInstance.autoGenerated = true;

            assembled.Add(generatedInstance);
            return generatedInstance;

        }

    }

}
