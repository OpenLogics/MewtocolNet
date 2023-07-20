using MewtocolNet.PublicEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MewtocolNet.RegisterBuilding {

    internal static class StepBaseTyper {

        /// <summary>
        /// Sets the register as a dotnet <see cref="System"/> type for direct conversion
        /// </summary>
        /// <typeparam name="T">
        /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
        /// </typeparam>
        internal static StepBase AsType<T>(this StepBase b) {

            if (!typeof(T).IsAllowedPlcCastingType()) {

                throw new NotSupportedException($"The dotnet type {typeof(T)}, is not supported for PLC type casting");

            }

            b.Data.dotnetVarType = typeof(T);

            return b;

        }

        /// <summary>
        /// Sets the register as a dotnet <see cref="System"/> type for direct conversion
        /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
        /// </summary>
        /// <param name="type">
        /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
        /// </param>
        internal static StepBase AsType(this StepBase b, Type type) {

            //for internal only, relay to AsType from string
            if (b.Data.buildSource == RegisterBuildSource.Attribute) {

                if ((type.IsArray || type == typeof(string)) && b.Data.typeDef != null) {

                    return b.AsType(b.Data.typeDef);

                } else if (type.IsArray && b.Data.typeDef == null) {

                    throw new NotSupportedException("Typedef parameter is needed for array types");

                } else if (b.Data.typeDef != null) {

                    throw new NotSupportedException("Can't use the typedef parameter on non array or string types");

                }

            }

            if (!type.IsAllowedPlcCastingType()) {

                throw new NotSupportedException($"The dotnet type {type}, is not supported for PLC type casting");

            }

            b.Data.dotnetVarType = type;

            return b;

        }

        /// <summary>
        /// Sets the register type as a predefined <see cref="PlcVarType"/>
        /// </summary>
        internal static StepBase AsType(this StepBase b, PlcVarType type) {

            b.Data.dotnetVarType = type.GetDefaultDotnetType();

            return b;

        }

        /// <summary>
        /// Sets the register type from the plc type string <br/>
        /// <c>Supported types:</c>
        /// <list type="bullet">
        /// <item><term>BOOL</term><description>Boolean R/X/Y registers</description></item>
        /// <item><term>INT</term><description>16 bit signed integer</description></item>
        /// <item><term>UINT</term><description>16 bit un-signed integer</description></item>
        /// <item><term>DINT</term><description>32 bit signed integer</description></item>
        /// <item><term>UDINT</term><description>32 bit un-signed integer</description></item>
        /// <item><term>REAL</term><description>32 bit floating point</description></item>
        /// <item><term>TIME</term><description>32 bit time interpreted as <see cref="TimeSpan"/></description></item>
        /// <item><term>STRING</term><description>String of chars, the interface will automatically get the length</description></item>
        /// <item><term>STRING[N]</term><description>String of chars, pre capped to N</description></item>
        /// <item><term>WORD</term><description>16 bit word interpreted as <see cref="ushort"/></description></item>
        /// <item><term>DWORD</term><description>32 bit double word interpreted as <see cref="uint"/></description></item>
        /// </list>
        /// </summary>
        internal static StepBase AsType(this StepBase b, string type) {

            var regexString = new Regex(@"^STRING *\[(?<len>[0-9]*)\]$", RegexOptions.IgnoreCase);
            var regexArray = new Regex(@"^ARRAY *\[(?<S1>[0-9]*)..(?<E1>[0-9]*)(?:\,(?<S2>[0-9]*)..(?<E2>[0-9]*))?(?:\,(?<S3>[0-9]*)..(?<E3>[0-9]*))?\] *OF {1,}(?<t>.*)$", RegexOptions.IgnoreCase);

            var stringMatch = regexString.Match(type);
            var arrayMatch = regexArray.Match(type);

            if (Enum.TryParse<PlcVarType>(type, out var parsed)) {

                b.Data.dotnetVarType = parsed.GetDefaultDotnetType();

            } else if (stringMatch.Success) {

                b.Data.dotnetVarType = typeof(string);
                b.Data.byteSizeHint = uint.Parse(stringMatch.Groups["len"].Value);

            } else if (arrayMatch.Success) {

                //invoke generic AsTypeArray

                string arrTypeString = arrayMatch.Groups["t"].Value;
                Type dotnetArrType = null;

                var stringMatchInArray = regexString.Match(arrTypeString);

                if (Enum.TryParse<PlcVarType>(arrTypeString, out var parsedArrType) && parsedArrType != PlcVarType.STRING) {

                    dotnetArrType = parsedArrType.GetDefaultDotnetType();


                } else if (stringMatchInArray.Success) {

                    dotnetArrType = typeof(string);
                    //Data.byteSizeHint = uint.Parse(stringMatch.Groups["len"].Value);

                } else {

                    throw new NotSupportedException($"The FP type '{arrTypeString}' was not recognized");

                }

                var indices = new List<int>();

                for (int i = 1; i < 4; i++) {

                    var arrStart = arrayMatch.Groups[$"S{i}"]?.Value;
                    var arrEnd = arrayMatch.Groups[$"E{i}"]?.Value;
                    if (string.IsNullOrEmpty(arrStart) || string.IsNullOrEmpty(arrEnd)) break;

                    var arrStartInt = int.Parse(arrStart);
                    var arrEndInt = int.Parse(arrEnd);

                    indices.Add(arrEndInt - arrStartInt + 1);

                }

                var arr = Array.CreateInstance(dotnetArrType, indices.ToArray());
                var arrType = arr.GetType();

                MethodInfo method = typeof(StepBaseTyper).GetMethod(nameof(AsTypeArray));
                MethodInfo generic = method.MakeGenericMethod(arrType);

                var tmp = (StepBase)generic.Invoke(null, new object[] {
                    b,
                    indices.ToArray()
                });

                return tmp;

            } else {

                throw new NotSupportedException($"The FP type '{type}' was not recognized");

            }

            return b;

        }

        /// <summary>
        /// Sets the register as a (multidimensional) array targeting a PLC array
        /// </summary>
        /// <typeparam name="T">
        /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
        /// </typeparam>
        /// <param name="indicies">
        /// Indicies for multi dimensional arrays, for normal arrays just one INT
        /// </param>
        /// <example>
        /// <b>One dimensional arrays:</b><br/>
        /// ARRAY [0..2] OF INT = <c>AsTypeArray&lt;short[]&gt;(3)</c><br/>
        /// ARRAY [5..6] OF DWORD = <c>AsTypeArray&lt;DWord[]&gt;(2)</c><br/>
        /// <br/>
        /// <b>Multi dimensional arrays:</b><br/>
        /// ARRAY [0..2, 0..3, 0..4] OF INT = <c>AsTypeArray&lt;short[,,]&gt;(3,4,5)</c><br/>
        /// ARRAY [5..6, 0..2] OF DWORD = <c>AsTypeArray&lt;DWord[,]&gt;(2, 3)</c><br/>
        /// </example>
        internal static StepBase AsTypeArray<T>(this StepBase b, params int[] indicies) {

            if (!typeof(T).IsArray)
                throw new NotSupportedException($"The type {typeof(T)} was no array");

            var arrRank = typeof(T).GetArrayRank();
            var elBaseType = typeof(T).GetElementType();

            if (arrRank > 3)
                throw new NotSupportedException($"4+ dimensional arrays are not supported");

            if (typeof(T) != typeof(byte[]) && !elBaseType.IsAllowedPlcCastingType())
                throw new NotSupportedException($"The dotnet type {typeof(T)}, is not supported for PLC array type casting");

            if (arrRank != indicies.Length)
                throw new NotSupportedException($"All dimensional array indicies must be set");

            b.Data.dotnetVarType = typeof(T);

            int byteSizePerItem = elBaseType.DetermineTypeByteIntialSize();
            int calcedTotalByteSize = indicies.Aggregate((a, x) => a * x) * byteSizePerItem;

            b.Data.byteSizeHint = (uint)calcedTotalByteSize;
            b.Data.arrayIndicies = indicies;

            if (b.Data.byteSizeHint % byteSizePerItem != 0) {
                throw new NotSupportedException($"The array element type {elBaseType} doesn't fit into the adress range");
            }

            return b;

        }

    }


}
