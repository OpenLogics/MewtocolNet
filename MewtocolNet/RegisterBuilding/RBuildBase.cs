using MewtocolNet.PublicEnums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MewtocolNet.RegisterBuilding {

    public class RBuildBase {

        protected internal MewtocolInterface attachedPLC;

        public RBuildBase() { }

        internal RBuildBase(MewtocolInterface plc) => attachedPLC = plc;

        internal List<BaseStepData> unfinishedList = new List<BaseStepData>();

        #region Parser stage

        //methods to test the input string on
        protected static List<Func<string, ParseResult>> parseMethods = new List<Func<string, ParseResult>>() {

            (x) => TryBuildBoolean(x),
            (x) => TryBuildNumericBased(x),
            (x) => TryBuildByteRangeBased(x),

        };

        public class SBase {

            public SBase() { }

            internal SBase(StepData data, RBuildBase bldr) {
                Data = data;
                builder = bldr;
            }

            internal StepData Data;

            internal RBuildBase builder;

        }

        internal protected struct ParseResult {

            internal ParseResultState state;

            internal string hardFailReason;

            internal BaseStepData stepData;

        }

        //bool registers
        private static ParseResult TryBuildBoolean(string plcAddrName) {

            //regex to find special register values
            var patternBool = new Regex(@"(?<prefix>X|Y|R)(?<area>[0-9]{0,3})(?<special>(?:[0-9]|[A-F]){1})?");

            var match = patternBool.Match(plcAddrName);

            if (!match.Success)
                return new ParseResult {
                    state = ParseResultState.FailedSoft
                };

            string prefix = match.Groups["prefix"].Value;
            string area = match.Groups["area"].Value;
            string special = match.Groups["special"].Value;

            IOType regType;
            uint areaAdd = 0;
            byte specialAdd = 0x0;

            //try cast the prefix
            if (!Enum.TryParse(prefix, out regType)) {

                return new ParseResult {
                    state = ParseResultState.FailedHard,
                    hardFailReason = $"Cannot parse '{plcAddrName}', the prefix is not allowed for boolean registers"
                };

            }

            if (!string.IsNullOrEmpty(area) && !uint.TryParse(area, out areaAdd)) {

                return new ParseResult {
                    state = ParseResultState.FailedHard,
                    hardFailReason = $"Cannot parse '{plcAddrName}', the area address: '{area}' is wrong"
                };

            }

            //special address not given
            if (string.IsNullOrEmpty(special) && !string.IsNullOrEmpty(area)) {

                var isAreaInt = uint.TryParse(area, NumberStyles.Number, CultureInfo.InvariantCulture, out var areaInt);

                if (isAreaInt && areaInt >= 0 && areaInt <= 9) {

                    //area address is actually meant as special address but 0-9
                    specialAdd = (byte)areaInt;
                    areaAdd = 0;


                } else if (isAreaInt && areaInt > 9) {

                    //area adress is meant to be the actual area address
                    areaAdd = areaInt;
                    specialAdd = 0;

                } else {

                    return new ParseResult {
                        state = ParseResultState.FailedHard,
                        hardFailReason = $"Cannot parse '{plcAddrName}', the special address: '{special}' is wrong 1",
                    };

                }

            } else {

                //special address parsed as hex num
                if (!byte.TryParse(special, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out specialAdd)) {

                    return new ParseResult {
                        state = ParseResultState.FailedHard,
                        hardFailReason = $"Cannot parse '{plcAddrName}', the special address: '{special}' is wrong 2",
                    };

                }

            }

            return new ParseResult {
                state = ParseResultState.Success,
                stepData = new StepData {
                    regType = (RegisterType)(int)regType,
                    memAddress = areaAdd,
                    specialAddress = specialAdd,
                }
            };

        }

        // one to two word registers
        private static ParseResult TryBuildNumericBased(string plcAddrName) {

            var patternByte = new Regex(@"^(?<prefix>DT|DDT)(?<area>[0-9]{1,5})$");

            var match = patternByte.Match(plcAddrName);

            if (!match.Success)
                return new ParseResult {
                    state = ParseResultState.FailedSoft
                };

            string prefix = match.Groups["prefix"].Value;
            string area = match.Groups["area"].Value;

            RegisterType regType;
            uint areaAdd = 0;

            //try cast the prefix
            if (!Enum.TryParse(prefix, out regType)) {

                return new ParseResult {
                    state = ParseResultState.FailedHard,
                    hardFailReason = $"Cannot parse '{plcAddrName}', the prefix is not allowed for numeric registers"
                };

            }

            if (!string.IsNullOrEmpty(area) && !uint.TryParse(area, out areaAdd)) {

                return new ParseResult {
                    state = ParseResultState.FailedHard,
                    hardFailReason = $"Cannot parse '{plcAddrName}', the area address: '{area}' is wrong"
                };

            }

            return new ParseResult {
                state = ParseResultState.Success,
                stepData = new StepData {
                    regType = regType,
                    memAddress = areaAdd,
                }
            };

        }

        // one to two word registers
        private static ParseResult TryBuildByteRangeBased(string plcAddrName) {

            var split = plcAddrName.Split('-');

            if (split.Length > 2)
                return new ParseResult {
                    state = ParseResultState.FailedHard,
                    hardFailReason = $"Cannot parse '{plcAddrName}', to many delimters '-'"
                };

            uint[] addresses = new uint[2];

            for (int i = 0; i < split.Length; i++) {

                string addr = split[i];
                var patternByte = new Regex(@"(?<prefix>DT|DDT)(?<area>[0-9]{1,5})");

                var match = patternByte.Match(addr);

                if (!match.Success)
                    return new ParseResult {
                        state = ParseResultState.FailedSoft
                    };

                string prefix = match.Groups["prefix"].Value;
                string area = match.Groups["area"].Value;

                RegisterType regType;
                uint areaAdd = 0;

                //try cast the prefix
                if (!Enum.TryParse(prefix, out regType)) {

                    return new ParseResult {
                        state = ParseResultState.FailedHard,
                        hardFailReason = $"Cannot parse '{plcAddrName}', the prefix is not allowed for word range registers"
                    };

                }

                if (!string.IsNullOrEmpty(area) && !uint.TryParse(area, out areaAdd)) {

                    return new ParseResult {
                        state = ParseResultState.FailedHard,
                        hardFailReason = $"Cannot parse '{plcAddrName}', the area address: '{area}' is wrong"
                    };

                }

                addresses[i] = areaAdd;

            }

            return new ParseResult {
                state = ParseResultState.Success,
                stepData = new StepData {
                    regType = RegisterType.DT_BYTE_RANGE,
                    wasAddressStringRangeBased = true,
                    dotnetVarType = typeof(byte[]),
                    memAddress = addresses[0],
                    byteSizeHint = (addresses[1] - addresses[0] + 1) * 2
                }
            };

        }

        #endregion

        #region Addressing stage

        internal StepData ParseAddress(string plcAddrName, string name = null) {

            foreach (var method in parseMethods) {

                var res = method.Invoke(plcAddrName);

                if (res.state == ParseResultState.Success) {

                    if (!string.IsNullOrEmpty(name)) res.stepData.name = name;

                    res.stepData.originalParseStr = plcAddrName;
                    res.stepData.buildSource = RegisterBuildSource.Manual;

                    return new StepData().Map(res.stepData);

                } else if (res.state == ParseResultState.FailedHard) {

                    throw new Exception(res.hardFailReason);

                }

            }

            throw new Exception("Wrong input format");

        }

        #endregion

        #region Typing stage

        public class SAddress : SBase {

            /// <summary>
            /// Sets the register as a dotnet <see cref="System"/> type for direct conversion
            /// </summary>
            /// <typeparam name="T">
            /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
            /// </typeparam>
            internal TypedRegister AsType<T>() {

                if (!typeof(T).IsAllowedPlcCastingType()) {

                    throw new NotSupportedException($"The dotnet type {typeof(T)}, is not supported for PLC type casting");

                }

                Data.dotnetVarType = typeof(T);

                return new TypedRegister().Map(this);

            }

            /// <summary>
            /// Sets the register as a dotnet <see cref="System"/> type for direct conversion
            /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
            /// </summary>
            /// <param name="type">
            /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
            /// </param>
            internal TypedRegister AsType(Type type) {

                //was ranged syntax array build
                if (Data.wasAddressStringRangeBased && type.IsArray && type.GetArrayRank() == 1) {

                    //invoke generic AsTypeArray
                    MethodInfo method = typeof(SAddress).GetMethod(nameof(AsTypeArray));
                    MethodInfo generic = method.MakeGenericMethod(type);

                    var elementType = type.GetElementType();

                    if (type != typeof(byte[]) && !elementType.IsAllowedPlcCastingType()) {

                        throw new NotSupportedException($"The dotnet type {elementType}, is not supported for PLC type casting");

                    }

                    int byteSizePerItem = elementType.DetermineTypeByteIntialSize();

                    //check if it fits without remainder
                    if (Data.byteSizeHint % byteSizePerItem != 0) {
                        throw new NotSupportedException($"The array element type {elementType} doesn't fit into the adress range");
                    }

                    return (TypedRegister)generic.Invoke(this, new object[] { 
                        //element count
                        new int[] { (int)((Data.byteSizeHint / byteSizePerItem)) }
                    });

                } else if (Data.wasAddressStringRangeBased) {

                    throw new NotSupportedException("DT range building is only allowed for 1 dimensional arrays");

                }

                //for internal only, relay to AsType from string
                if (Data.buildSource == RegisterBuildSource.Attribute) {

                    if ((type.IsArray || type == typeof(string)) && Data.typeDef != null) {

                        return AsType(Data.typeDef);

                    } else if (type.IsArray && Data.typeDef == null) {

                        throw new NotSupportedException("Typedef parameter is needed for array types");

                    } else if (Data.typeDef != null) {

                        throw new NotSupportedException("Can't use the typedef parameter on non array or string types");

                    }

                }

                if (!type.IsAllowedPlcCastingType()) {

                    throw new NotSupportedException($"The dotnet type {type}, is not supported for PLC type casting");

                }

                Data.dotnetVarType = type;

                return new TypedRegister().Map(this);

            }

            /// <summary>
            /// Sets the register type as a predefined <see cref="PlcVarType"/>
            /// </summary>
            internal TypedRegister AsType(PlcVarType type) {

                Data.dotnetVarType = type.GetDefaultDotnetType();

                return new TypedRegister().Map(this);

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
            internal TypedRegister AsType(string type) {

                var regexString = new Regex(@"^STRING *\[(?<len>[0-9]*)\]$", RegexOptions.IgnoreCase);
                var regexArray = new Regex(@"^ARRAY *\[(?<S1>[0-9]*)..(?<E1>[0-9]*)(?:\,(?<S2>[0-9]*)..(?<E2>[0-9]*))?(?:\,(?<S3>[0-9]*)..(?<E3>[0-9]*))?\] *OF {1,}(?<t>.*)$", RegexOptions.IgnoreCase);

                var stringMatch = regexString.Match(type);
                var arrayMatch = regexArray.Match(type);

                if (Enum.TryParse<PlcVarType>(type, out var parsed)) {

                    Data.dotnetVarType = parsed.GetDefaultDotnetType();

                } else if (stringMatch.Success) {

                    Data.dotnetVarType = typeof(string);
                    Data.byteSizeHint = uint.Parse(stringMatch.Groups["len"].Value);

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

                    MethodInfo method = typeof(SAddress).GetMethod(nameof(AsTypeArray));
                    MethodInfo generic = method.MakeGenericMethod(arrType);

                    var tmp = (TypedRegister)generic.Invoke(this, new object[] {
                        indices.ToArray()
                    });

                    tmp.builder = builder;
                    tmp.Data = Data;

                    return tmp;

                } else {

                    throw new NotSupportedException($"The FP type '{type}' was not recognized");

                }

                return new TypedRegister().Map(this);

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
            internal TypedRegister AsTypeArray<T>(params int[] indicies) {

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

                Data.dotnetVarType = typeof(T);

                int byteSizePerItem = elBaseType.DetermineTypeByteIntialSize();
                int calcedTotalByteSize = indicies.Aggregate((a, x) => a * x) * byteSizePerItem;

                Data.byteSizeHint = (uint)calcedTotalByteSize;
                Data.arrayIndicies = indicies;

                if (Data.byteSizeHint % byteSizePerItem != 0) {
                    throw new NotSupportedException($"The array element type {elBaseType} doesn't fit into the adress range");
                }

                return new TypedRegister().Map(this);

            }

        }

        #endregion

        #region Typing size hint

        public class TypedRegister : SBase {

            public OptionsRegister SizeHint(int hint) {

                Data.byteSizeHint = (uint)hint;

                return new OptionsRegister().Map(this);

            }

            public OptionsRegister PollLevel(int level) {

                Data.pollLevel = level;

                return new OptionsRegister().Map(this); 

            }

        }

        #endregion

        #region Options stage

        public class OptionsRegister : SBase {

            internal OptionsRegister() { }

            internal OptionsRegister(StepData data, RBuildBase bldr) : base(data, bldr) { }

            /// <summary>
            /// Sets the poll level of the register
            /// </summary>
            public OptionsRegister PollLevel(int level) {

                Data.pollLevel = level;

                return this;    

            }

        }

        #endregion

    }

}
