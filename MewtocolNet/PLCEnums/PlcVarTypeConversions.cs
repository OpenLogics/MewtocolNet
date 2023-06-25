using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MewtocolNet {
    internal static class PlcVarTypeConversions {

        static Dictionary<PlcVarType, Type> dictTypeConv = new Dictionary<PlcVarType, Type> {

            { PlcVarType.BOOL, typeof(bool) },
            { PlcVarType.INT, typeof(short) },
            { PlcVarType.UINT, typeof(ushort) },
            { PlcVarType.DINT, typeof(int) },
            { PlcVarType.UDINT, typeof(uint) },
            { PlcVarType.REAL, typeof(float) },
            { PlcVarType.TIME, typeof(TimeSpan) },
            { PlcVarType.STRING, typeof(string) },

        };

        static Dictionary<PlcVarType, Type> dictRegisterConv = new Dictionary<PlcVarType, Type> {

            { PlcVarType.BOOL, typeof(BRegister) },
            { PlcVarType.INT, typeof(NRegister<short>) },
            { PlcVarType.UINT, typeof(NRegister<ushort>) },
            { PlcVarType.DINT, typeof(NRegister<int>) },
            { PlcVarType.UDINT, typeof(NRegister<uint>) },
            { PlcVarType.REAL, typeof(NRegister<float>) },
            { PlcVarType.TIME, typeof(NRegister<TimeSpan>) },
            { PlcVarType.STRING, typeof(SRegister) },

        };

        internal static bool IsAllowedPlcCastingType <T> () {

            var inversed = dictTypeConv.ToDictionary((i) => i.Value, (i) => i.Key);

            return inversed.ContainsKey(typeof(T));

        }

        internal static bool IsAllowedPlcCastingType (this Type type) {

            var inversed = dictTypeConv.ToDictionary((i) => i.Value, (i) => i.Key);

            return inversed.ContainsKey(type);

        }

        internal static Type ToDotnetType (this PlcVarType type) { 
        
            if(dictTypeConv.ContainsKey(type)) {  
            
                return dictTypeConv[type];  
            
            }

            throw new NotSupportedException($"The PlcVarType: '{type}' is not supported");

        }

        internal static PlcVarType ToPlcVarType (this Type type) {

            var inversed = dictTypeConv.ToDictionary((i) => i.Value, (i) => i.Key);

            if (inversed.ContainsKey(type)) {

                return inversed[type];

            }

            throw new NotSupportedException($"The Dotnet Type: '{type}' is not supported");

        }

        internal static Type ToRegisterType (this PlcVarType type) {

            if (dictRegisterConv.ContainsKey(type)) {

                return dictRegisterConv[type];

            }

            throw new NotSupportedException($"The PlcVarType: '{type}' is not supported");

        }

    }

}
