using MewtocolNet.RegisterAttributes;
using MewtocolNet.UnderlyingRegisters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

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

            internal RegisterCollection regCollection;
            internal PropertyInfo boundProperty;

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

        #endregion

        #region Type determination stage

        public class SAddress : SBase {

            /// <summary>
            /// Sets the register as a dotnet <see cref="System"/> type for direct conversion
            /// <list type="bullet">
            /// <item><term><see cref="bool"/></term><description>Boolean R/X/Y registers</description></item>
            /// <item><term><see cref="short"/></term><description>16 bit signed integer</description></item>
            /// <item><term><see cref="ushort"/></term><description>16 bit un-signed integer</description></item>
            /// <item><term><see cref="Word"/></term><description>16 bit word (2 bytes)</description></item>
            /// <item><term><see cref="int"/></term><description>32 bit signed integer</description></item>
            /// <item><term><see cref="uint"/></term><description>32 bit un-signed integer</description></item>
            /// <item><term><see cref="DWord"/></term><description>32 bit word (4 bytes)</description></item>
            /// <item><term><see cref="float"/></term><description>32 bit floating point</description></item>
            /// <item><term><see cref="TimeSpan"/></term><description>32 bit time from <see cref="PlcVarType.TIME"/> interpreted as <see cref="TimeSpan"/></description></item>
            /// <item><term><see cref="Enum"/></term><description>16 or 32 bit enums, also supports flags</description></item>
            /// <item><term><see cref="string"/></term><description>String of chars, the interface will automatically get the length</description></item>
            /// <item><term><see cref="byte[]"/></term><description>As an array of bytes</description></item>
            /// </list>
            /// </summary>
            public TempRegister<T> AsType<T> () {

                if (!typeof(T).IsAllowedPlcCastingType()) {

                    throw new NotSupportedException($"The dotnet type {typeof(T)}, is not supported for PLC type casting");

                }

                Data.dotnetVarType = typeof(T);

                return new TempRegister<T>(Data, builder);

            }

            ///<inheritdoc cref="AsType{T}()"/>
            public TempRegister AsType (Type type) {

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
            public TempRegister AsType(string type) {

                var stringMatch = Regex.Match(type, @"STRING *\[(?<len>[0-9]*)\]", RegexOptions.IgnoreCase);
                var arrayMatch = Regex.Match(type, @"ARRAY *\[(?<S1>[0-9]*)..(?<E1>[0-9]*)(?:\,(?<S2>[0-9]*)..(?<E2>[0-9]*))?(?:\,(?<S3>[0-9]*)..(?<E3>[0-9]*))?\] *OF {1,}(?<t>.*)", RegexOptions.IgnoreCase);

                if (Enum.TryParse<PlcVarType>(type, out var parsed)) {

                    Data.dotnetVarType = parsed.GetDefaultDotnetType();

                } else if (stringMatch.Success) {

                    Data.dotnetVarType = typeof(string);
                    Data.stringSize = int.Parse(stringMatch.Groups["len"].Value);

                } else if (arrayMatch.Success) {

                    throw new NotSupportedException("Arrays are currently not supported");

                } else {

                    throw new NotSupportedException($"The mewtocol type '{type}' was not recognized");
                
                }

                return new TempRegister(Data, builder);

            }

            /// <summary>
            /// Gets the data DT area as a <see cref="byte[]"/>
            /// </summary>
            /// <param name="byteLength">Bytes to assign</param>
            public TempRegister AsBytes (uint byteLength) {

                if (Data.regType != RegisterType.DT) {

                    throw new NotSupportedException($"Cant use the {nameof(AsBytes)} converter on a non {nameof(RegisterType.DT)} register");

                }

                Data.byteSize = byteLength;
                Data.dotnetVarType = typeof(byte[]);

                return new TempRegister(Data, builder);

            }

            /// <summary>
            /// Gets the data DT area as a <see cref="BitArray"/>
            /// </summary>
            /// <param name="bitCount">Number of bits to read</param>
            public TempRegister AsBits (ushort bitCount = 16) {

                if (Data.regType != RegisterType.DT) {

                    throw new NotSupportedException($"Cant use the {nameof(AsBits)} converter on a non {nameof(RegisterType.DT)} register");

                }

                Data.bitSize = bitCount;
                Data.dotnetVarType = typeof(BitArray);

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
