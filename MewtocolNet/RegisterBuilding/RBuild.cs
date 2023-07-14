using MewtocolNet.PublicEnums;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.UnderlyingRegisters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static MewtocolNet.RegisterBuilding.RBuild;

namespace MewtocolNet.RegisterBuilding {

    internal enum ParseResultState {

        /// <summary>
        /// The parse try failed at the intial regex match
        /// </summary>
        FailedSoft,
        /// <summary>
        /// The parse try failed at the afer- regex match
        /// </summary>
        FailedHard,
        /// <summary>
        /// The parse try did work
        /// </summary>
        Success,

    }

    /// <summary>
    /// Contains useful tools for register creation
    /// </summary>
    public class RBuild {

        private MewtocolInterface attachedPLC;

        public RBuild () { }

        internal RBuild (MewtocolInterface plc) {

            attachedPLC = plc;

        }

        public static RBuild Factory => new RBuild();

        internal List<SData> unfinishedList = new List<SData>();       

        #region String parse stage

        //methods to test the input string on
        private static List<Func<string, ParseResult>> parseMethods = new List<Func<string, ParseResult>>() {

            (x) => TryBuildBoolean(x),
            (x) => TryBuildNumericBased(x),
            (x) => TryBuildByteRangeBased(x),

        };

        internal class SData {

            internal RegisterBuildSource buildSource = RegisterBuildSource.Anonymous;

            internal bool wasAddressStringRangeBased;
            internal string originalParseStr;
            internal string name;
            internal RegisterType regType;
            internal uint memAddress;
            internal byte specialAddress;
            internal Type dotnetVarType;

            //optional
            internal uint? byteSize;
            internal uint? bitSize;
            internal int? stringSize;

            internal int pollLevel = 1;

            //only for building from attributes
            internal RegisterCollection regCollection;
            internal PropertyInfo boundProperty;

            internal string typeDef;

        }

        public class SBase {

            public SBase() { }

            internal SBase(SData data, RBuild bldr) {
                Data = data;
                builder = bldr;
            }

            internal SData Data { get; set; }

            internal RBuild builder;

        }

        internal struct ParseResult {

            public ParseResultState state;

            public string hardFailReason;

            public SData stepData;

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
                stepData = new SData {
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
                stepData = new SData {
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
                if (!Enum.TryParse(prefix, out regType) || regType != RegisterType.DT) {

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
                stepData = new SData {
                    regType = RegisterType.DT_BYTE_RANGE,
                    wasAddressStringRangeBased = true,
                    dotnetVarType = typeof(byte[]),
                    memAddress = addresses[0],
                    byteSize = (addresses[1] - addresses[0] + 1) * 2
                }
            };

        }

        /// <summary>
        /// Starts the register builder for a new mewtocol address <br/>
        /// Examples:
        /// <code>Address("DT100") | Address("R10A") | Address("DDT50", "MyRegisterName")</code>
        /// </summary>
        /// <param name="plcAddrName">Address name formatted as FP-Address like in FP-Winpro</param>
        /// <param name="name">Custom name for the register to referr to it later</param>
        public SAddress Address (string plcAddrName, string name = null) {

            foreach (var method in parseMethods) {

                var res = method.Invoke(plcAddrName);

                if(res.state == ParseResultState.Success) {

                    if (!string.IsNullOrEmpty(name)) res.stepData.name = name;

                    res.stepData.originalParseStr = plcAddrName;
                    res.stepData.buildSource = RegisterBuildSource.Manual;

                    unfinishedList.Add(res.stepData);

                    return new SAddress {
                        Data = res.stepData,
                        builder = this,
                    };

                } else if(res.state == ParseResultState.FailedHard) {

                    throw new Exception(res.hardFailReason);

                }

            }

            throw new Exception("Wrong input format");

        }

        //internal use only, adds a type definition (for use when building from attibute)
        internal SAddress AddressFromAttribute (string plcAddrName, string typeDef) {

            var built = Address(plcAddrName);
            built.Data.typeDef = typeDef;
            built.Data.buildSource = RegisterBuildSource.Attribute;
            return built;

        }

        #endregion

        #region Type determination stage

        public class SAddress : SBase {

            /// <summary>
            /// Sets the register as a dotnet <see cref="System"/> type for direct conversion
            /// </summary>
            /// <typeparam name="T">
            /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
            /// </typeparam>
            public TempRegister<T> AsType<T> () {

                if (!typeof(T).IsAllowedPlcCastingType()) {

                    throw new NotSupportedException($"The dotnet type {typeof(T)}, is not supported for PLC type casting");

                }

                Data.dotnetVarType = typeof(T);

                return new TempRegister<T>(Data, builder);

            }

            /// <summary>
            /// Sets the register as a dotnet <see cref="System"/> type for direct conversion
            /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
            /// </summary>
            /// <param name="type">
            /// <include file="../Documentation/docs.xml" path='extradoc/class[@name="support-conv-types"]/*' />
            /// </param>
            public TempRegister AsType (Type type) {

                //was ranged syntax array build
                if (Data.wasAddressStringRangeBased && type.IsArray && type.GetArrayRank() == 1) {

                    //invoke generic AsTypeArray
                    MethodInfo method = typeof(SAddress).GetMethod(nameof(AsTypeArray));
                    MethodInfo generic = method.MakeGenericMethod(type);

                    var elementType = type.GetElementType();

                    if (!elementType.IsAllowedPlcCastingType()) {

                        throw new NotSupportedException($"The dotnet type {elementType}, is not supported for PLC type casting");

                    }

                    bool isExtensionTypeDT = typeof(MewtocolExtensionTypeDT).IsAssignableFrom(elementType);
                    bool isExtensionTypeDDT = typeof(MewtocolExtensionTypeDDT).IsAssignableFrom(elementType);

                    int byteSizePerItem = 0;
                    if(elementType.Namespace.StartsWith("System")) {
                        byteSizePerItem = Marshal.SizeOf(elementType);
                    } else if (isExtensionTypeDT) {
                        byteSizePerItem = 2;
                    } else if (isExtensionTypeDDT) {
                        byteSizePerItem = 4;
                    }

                    //check if it fits without remainder
                    if(Data.byteSize % byteSizePerItem != 0) {
                        throw new NotSupportedException($"The array element type {elementType} doesn't fit into the adress range");
                    }

                    return (TempRegister)generic.Invoke(this, new object[] { 
                        //element count
                        new int[] { (int)((Data.byteSize / byteSizePerItem) / 2) }
                    });

                } else if(Data.wasAddressStringRangeBased) {

                    throw new NotSupportedException("DT range building is only allowed for 1 dimensional arrays");

                }

                //for internal only, relay to AsType from string
                if (Data.buildSource == RegisterBuildSource.Attribute) {

                    if ((type.IsArray || type == typeof(string)) && Data.typeDef != null) {

                        return AsType(Data.typeDef);

                    } else if ((type.IsArray || type == typeof(string)) && Data.typeDef == null) {

                        throw new NotSupportedException("Typedef parameter is needed for array or string types");

                    } else if (Data.typeDef != null) {

                        throw new NotSupportedException("Can't use the typedef parameter on non array or string types");

                    }

                }
                
                if (!type.IsAllowedPlcCastingType()) {

                    throw new NotSupportedException($"The dotnet type {type}, is not supported for PLC type casting");

                }

                Data.dotnetVarType = type;

                return new TempRegister(Data, builder);

            }

            /// <summary>
            /// Sets the register type as a predefined <see cref="PlcVarType"/>
            /// </summary>
            public TempRegister AsType (PlcVarType type) {

                Data.dotnetVarType = type.GetDefaultDotnetType();

                return new TempRegister(Data, builder);

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
            public TempRegister AsType (string type) {

                var stringMatch = Regex.Match(type, @"STRING *\[(?<len>[0-9]*)\]", RegexOptions.IgnoreCase);
                var arrayMatch = Regex.Match(type, @"ARRAY *\[(?<S1>[0-9]*)..(?<E1>[0-9]*)(?:\,(?<S2>[0-9]*)..(?<E2>[0-9]*))?(?:\,(?<S3>[0-9]*)..(?<E3>[0-9]*))?\] *OF {1,}(?<t>.*)", RegexOptions.IgnoreCase);

                if (Enum.TryParse<PlcVarType>(type, out var parsed)) {

                    Data.dotnetVarType = parsed.GetDefaultDotnetType();

                } else if (stringMatch.Success) {

                    Data.dotnetVarType = typeof(string);
                    Data.stringSize = int.Parse(stringMatch.Groups["len"].Value);

                } else if (arrayMatch.Success) {

                    //invoke generic AsTypeArray

                    string arrTypeString = arrayMatch.Groups["t"].Value;

                    if (Enum.TryParse<PlcVarType>(arrTypeString, out var parsedArrType)) {

                        var dotnetArrType = parsedArrType.GetDefaultDotnetType();
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

                        return (TempRegister)generic.Invoke(this, new object[] {
                            indices.ToArray()
                        });

                    } else {

                        throw new NotSupportedException($"The FP type '{arrTypeString}' was not recognized");
                    }

                } else {

                    throw new NotSupportedException($"The FP type '{type}' was not recognized");
                
                }

                return new TempRegister(Data, builder);

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
            public TempRegister AsTypeArray<T> (params int[] indicies) {

                if (!typeof(T).IsArray)
                    throw new NotSupportedException($"The type {typeof(T)} was no array");

                var arrRank = typeof(T).GetArrayRank();
                var elBaseType = typeof(T).GetElementType();

                if (arrRank > 3)
                    throw new NotSupportedException($"4+ dimensional arrays are not supported");

                if (!elBaseType.IsAllowedPlcCastingType())
                    throw new NotSupportedException($"The dotnet type {typeof(T)}, is not supported for PLC array type casting");

                if (arrRank != indicies.Length)
                    throw new NotSupportedException($"All dimensional array indicies must be set");

                Data.dotnetVarType = typeof(T);

                return new TempRegister(Data, builder);

            }

            /// <summary>
            /// Automatically finds the best type for the register
            /// </summary>
            public TempRegister AutoType() {

                switch (Data.regType) {
                    case RegisterType.X:
                    case RegisterType.Y:
                    case RegisterType.R:
                    Data.dotnetVarType = typeof(bool);
                    break;
                    case RegisterType.DT:
                    Data.dotnetVarType = typeof(short);
                    break;
                    case RegisterType.DDT:
                    Data.dotnetVarType = typeof(int);
                    break;
                    case RegisterType.DT_BYTE_RANGE:
                    Data.dotnetVarType = typeof(string);
                    break;
                }

                return new TempRegister(Data, builder);

            }

        }

        #endregion

        #region Options stage

        public class TempRegister<T> : SBase {

            internal TempRegister(SData data, RBuild bldr) : base(data, bldr) {}

            /// <summary>
            /// Sets the poll level of the register
            /// </summary>
            public TempRegister<T> PollLevel (int level) {

                Data.pollLevel = level;

                return this;

            }

            /// <summary>
            /// Writes data to the register and bypasses the memory manager <br/>
            /// </summary>
            /// <param name="value">The value to write</param>
            /// <returns>True if success</returns>
            public async Task<bool> WriteToAsync (T value) => await builder.WriteAnonymousAsync(this, value);

            /// <summary>
            /// Reads data from the register and bypasses the memory manager <br/>
            /// </summary>
            /// <returns>The value read or null if failed</returns>
            public async Task<T> ReadFromAsync () => await builder.ReadAnonymousAsync(this);

        }

        public class TempRegister : SBase {

            internal TempRegister(SData data, RBuild bldr) : base(data, bldr) { }

            /// <summary>
            /// Sets the poll level of the register
            /// </summary>
            public TempRegister PollLevel (int level) {

                Data.pollLevel = level;

                return this;

            }

            /// <summary>
            /// Writes data to the register and bypasses the memory manager <br/>
            /// </summary>
            /// <param name="value">The value to write</param>
            /// <returns>True if success</returns>
            public async Task<bool> WriteToAsync(object value) => await builder.WriteAnonymousAsync(this, value);

            /// <summary>
            /// Reads data from the register and bypasses the memory manager <br/>
            /// </summary>
            /// <returns>The value read or null if failed</returns>
            public async Task<object> ReadFromAsync () => await builder.ReadAnonymousAsync(this);

            internal TempRegister RegCollection(RegisterCollection col) {

                Data.regCollection = col;

                return this;

            }

            internal TempRegister BoundProp(PropertyInfo prop) {

                Data.boundProperty = prop;

                return this;

            }

        }

        #endregion

        #region Anonymous read/write bindings

        private async Task<bool> WriteAnonymousAsync (TempRegister reg, object value) {

            var assembler = new RegisterAssembler(attachedPLC);
            var tempRegister = assembler.Assemble(reg.Data);
            return await tempRegister.WriteAsync(value);

        }

        private async Task<bool> WriteAnonymousAsync<T>(TempRegister<T> reg, object value) {

            var assembler = new RegisterAssembler(attachedPLC);
            var tempRegister = assembler.Assemble(reg.Data);
            return await tempRegister.WriteAsync(value);

        }

        private async Task<object> ReadAnonymousAsync (TempRegister reg) {

            var assembler = new RegisterAssembler(attachedPLC);
            var tempRegister = assembler.Assemble(reg.Data);
            return await tempRegister.ReadAsync();

        }

        private async Task<T> ReadAnonymousAsync<T>(TempRegister<T> reg) {

            var assembler = new RegisterAssembler(attachedPLC);
            var tempRegister = assembler.Assemble(reg.Data);
            return (T)await tempRegister.ReadAsync();

        }

        #endregion

    }

}
