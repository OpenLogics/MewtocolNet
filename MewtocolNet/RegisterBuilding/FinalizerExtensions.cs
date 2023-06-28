using MewtocolNet.Registers;
using System;
using System.Linq;
using System.Reflection;

namespace MewtocolNet.RegisterBuilding {

    public static class FinalizerExtensions {

        public static IRegister Build(this RegisterBuilderStep step) {

            //if no casting method in builder was called => autocast the type from the RegisterType
            if (!step.wasCasted)
                step.AutoType();

            //fallbacks if no casting builder was given
            step.GetFallbackDotnetType();

            var builtReg = new RegisterBuildInfo {

                name = step.Name,
                specialAddress = step.SpecialAddress,
                memoryAddress = step.MemAddress,
                memorySizeBytes = step.MemByteSize,
                registerType = step.RegType,
                dotnetCastType = step.dotnetVarType,

            }.Build();

            step.AddToRegisterList(builtReg);

            return builtReg;

        }

        private static void GetFallbackDotnetType(this RegisterBuilderStep step) {

            bool isBoolean = step.RegType.IsBoolean();
            bool isTypeNotDefined = step.plcVarType == null && step.dotnetVarType == null;

            if (isTypeNotDefined && step.RegType == RegisterType.DT) {

                step.dotnetVarType = typeof(short);

            }
            if (isTypeNotDefined && step.RegType == RegisterType.DDT) {

                step.dotnetVarType = typeof(int);

            } else if (isTypeNotDefined && isBoolean) {

                step.dotnetVarType = typeof(bool);

            } else if (isTypeNotDefined && step.RegType == RegisterType.DT_BYTE_RANGE) {

                step.dotnetVarType = typeof(string);

            }

            if (step.plcVarType != null) {

                step.dotnetVarType = step.plcVarType.Value.GetDefaultDotnetType();

            }

        }

        private static void AddToRegisterList(this RegisterBuilderStep step, BaseRegister instance) {

            if (step.forInterface == null) return;

            step.forInterface.AddRegister(instance);

        }

    }

}
