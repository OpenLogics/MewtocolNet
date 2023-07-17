using MewtocolNet.Registers;
using System;
using System.Collections.Generic;

namespace MewtocolNet {

    internal static class PlcVarTypeConversions {

        static List<Type> allowedCastingTypes = PlcValueParser.GetAllowDotnetTypes();

        static List<Type> allowedGenericRegisters = PlcValueParser.GetAllowRegisterTypes();

        internal static bool IsAllowedRegisterGenericType(this IRegister register) {

            return allowedGenericRegisters.Contains(register.GetType());

        }

        internal static bool IsAllowedPlcCastingType<T>() {

            if (typeof(T).IsEnum) return true;

            return allowedCastingTypes.Contains(typeof(T));

        }

        internal static bool IsAllowedPlcCastingType(this Type type) {

            if (type.IsEnum || type == typeof(string)) return true;

            return allowedCastingTypes.Contains(type);

        }

        internal static RegisterType ToRegisterTypeDefault(this Type type) {

            if (type.IsEnum) return RegisterType.DT;

            var found = PlcValueParser.GetDefaultRegisterType(type);

            if (found != null) {

                return found.Value;

            }

            throw new Exception("No default register type found");

        }

        internal static PlcVarType ToPlcVarType(this Type type) {

            var found = type.GetDefaultPlcVarType().Value;

            if (found != null) {

                return found;

            }

            throw new Exception("No default plcvar type found");

        }

    }

}
