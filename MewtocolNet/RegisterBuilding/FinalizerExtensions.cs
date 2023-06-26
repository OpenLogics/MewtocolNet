using MewtocolNet.Registers;
using System;
using System.Linq;
using System.Reflection;

namespace MewtocolNet.RegisterBuilding
{

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

            } else if (isTypeNotDefined && step.RegType == RegisterType.DT_RANGE) {

                step.dotnetVarType = typeof(string);

            }

            if(step.plcVarType != null) {

                step.dotnetVarType = step.plcVarType.Value.GetDefaultDotnetType();

            }

            var builtReg = new RegisterBuildInfo {

                name = step.Name,
                specialAddress = step.SpecialAddress,
                memoryAddress = step.MemAddress,
                registerType = step.RegType,
                dotnetCastType = step.dotnetVarType,

            }.Build();

            step.AddToRegisterList(builtReg);

            return builtReg;

        }

        private static void AddToRegisterList (this RegisterBuilderStep step, IRegister instance) {

            if (step.forInterface == null) return;

            step.forInterface.AddRegister(instance);

        }

    }

}
