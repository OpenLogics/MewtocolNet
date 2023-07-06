using MewtocolNet.Exceptions;
using MewtocolNet.Registers;
using MewtocolNet.TypeConversion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MewtocolNet {

    internal static class PlcVarTypeConversions {

        static List<Type> allowedCastingTypes = PlcValueParser.GetAllowDotnetTypes();

        static List<Type> allowedGenericRegisters = PlcValueParser.GetAllowRegisterTypes();

        internal static bool IsAllowedRegisterGenericType(this IRegister register) {

            return allowedGenericRegisters.Contains(register.GetType());

        }

        internal static bool IsAllowedPlcCastingType<T>() {

            return allowedCastingTypes.Contains(typeof(T));

        }

        internal static bool IsAllowedPlcCastingType(this Type type) {

            return allowedCastingTypes.Contains(type);

        }

        internal static RegisterType ToRegisterTypeDefault(this Type type) {

            var found = PlcValueParser.GetDefaultRegisterType(type);

            if (found != null) {

                return found.Value;

            }

            throw new MewtocolException("No default register type found");

        }

        internal static PlcVarType ToPlcVarType (this Type type) {

            var found = type.GetDefaultPlcVarType().Value;

            if (found != null) {

                return found;

            }

            throw new MewtocolException("No default plcvar type found");

        }

    }

}
