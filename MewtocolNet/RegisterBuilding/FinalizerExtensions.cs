using MewtocolNet.Registers;
using System;
using System.Reflection;

namespace MewtocolNet.RegisterBuilding {

    public static class FinalizerExtensions {

        public static IRegister Build (this RegisterBuilderStep step) {

            //if no casting method in builder was called => autocast the type from the RegisterType
            if (!step.wasCasted)
                step.AutoType();

            bool isBoolean = step.RegType.IsBoolean();
            bool isTypeNotDefined = step.plcVarType == null && step.dotnetVarType == null;

            //fallbacks if no casting builder was given
            if (isTypeNotDefined && step.RegType == RegisterType.DT) {

                step.dotnetVarType = typeof(short);

            }
            if (isTypeNotDefined && step.RegType == RegisterType.DDT) {

                step.dotnetVarType = typeof(int);

            } else if (isTypeNotDefined && isBoolean) {

                step.dotnetVarType = typeof(bool);

            } else if (isTypeNotDefined && step.RegType == RegisterType.DT_START) {

                step.dotnetVarType = typeof(string);

            }

            if(step.plcVarType != null) {

                step.dotnetVarType = step.plcVarType.Value.ToDotnetType();

            }

            //as numeric register
            if (step.RegType.IsNumericDTDDT()) {

                if(step.plcVarType == null && step.dotnetVarType != null) {

                    step.plcVarType = step.dotnetVarType.ToPlcVarType();

                }

                var type = step.plcVarType.Value.ToRegisterType();

                var areaAddr = step.MemAddress;
                var name = step.Name;

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;

                //int _adress, string _name = null, bool isBitwise = false, Type _enumType = null
                var parameters = new object[] { areaAddr, name, false, null };
                var instance = (IRegister)Activator.CreateInstance(type, flags, null, parameters, null);

                return instance;

            }

            if (step.RegType.IsBoolean()) {

                var io = (IOType)(int)step.RegType;
                var spAddr = step.SpecialAddress;
                var areaAddr = step.MemAddress;
                var name = step.Name;

                //create a new bregister instance
                var flags = BindingFlags.Public | BindingFlags.Instance;
                var parameters = new object[] { io, spAddr.Value, areaAddr, name };
                var instance = (BRegister)Activator.CreateInstance(typeof(BRegister), flags, null, parameters, null);

                return instance;

            }

            if (step.dotnetVarType != null) {



            }

            throw new Exception("Failed to build register");

        }

    }

}
