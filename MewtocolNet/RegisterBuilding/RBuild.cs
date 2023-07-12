using MewtocolNet.Exceptions;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using MewtocolNet.UnderlyingRegisters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

            internal int pollLevel = 1;

            internal RegisterCollection regCollection;
            internal PropertyInfo boundProperty;

        }

        public class SBase {

            public SBase() { }

            internal SBase(SData data) {
                Data = data;
            }

            internal SData Data { get; set; }

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

        public SAddress Address (string plcAddrName, string name = null) {

            foreach (var method in parseMethods) {

                var res = method.Invoke(plcAddrName);

                if(res.state == ParseResultState.Success) {

                    if (!string.IsNullOrEmpty(name)) res.stepData.name = name;

                    res.stepData.originalParseStr = plcAddrName;

                    unfinishedList.Add(res.stepData);

                    return new SAddress {
                        Data = res.stepData
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

            public TempRegister<T> AsType<T> () {

                if (!typeof(T).IsAllowedPlcCastingType()) {

                    throw new NotSupportedException($"The dotnet type {typeof(T)}, is not supported for PLC type casting");

                }

                Data.dotnetVarType = typeof(T);

                return new TempRegister<T>(Data);

            }

            public TempRegister AsType (Type type) {

                if (!type.IsAllowedPlcCastingType()) {

                    throw new NotSupportedException($"The dotnet type {type}, is not supported for PLC type casting");

                }

                Data.dotnetVarType = type;

                return new TempRegister(Data);

            }

            public TempRegister AsBytes (uint byteLength) {

                if (Data.regType != RegisterType.DT) {

                    throw new NotSupportedException($"Cant use the {nameof(AsBytes)} converter on a non {nameof(RegisterType.DT)} register");

                }

                Data.byteSize = byteLength;
                Data.dotnetVarType = typeof(byte[]);

                return new TempRegister(Data);

            }

            public TempRegister AsBits (ushort bitCount = 16) {

                if (Data.regType != RegisterType.DT) {

                    throw new NotSupportedException($"Cant use the {nameof(AsBits)} converter on a non {nameof(RegisterType.DT)} register");

                }

                Data.bitSize = bitCount;
                Data.dotnetVarType = typeof(BitArray);

                return new TempRegister(Data);

            }

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

                return new TempRegister(Data);

            }

        }

        #endregion

        #region Options stage

        public class TempRegister<T> : SBase {

            internal TempRegister(SData data) : base(data) {}

            public TempRegister<T> PollLevel (int level) {

                Data.pollLevel = level;

                return this;

            }

            public async Task WriteToAsync (T value) => throw new NotImplementedException();

        }

        public class TempRegister : SBase {

            internal TempRegister(SData data) : base(data) { }

            public TempRegister PollLevel (int level) {

                Data.pollLevel = level;

                return this;

            }

            internal TempRegister RegCollection (RegisterCollection col) {

                Data.regCollection = col;

                return this;

            }

            internal TempRegister BoundProp (PropertyInfo prop) {

                Data.boundProperty = prop;

                return this;

            }

            public async Task WriteToAsync (object value) => throw new NotImplementedException();

        }

        #endregion

    }

    internal class RegisterAssembler {

        internal RegisterCollection collectionTarget;

        internal MewtocolInterface onInterface;

        internal RegisterAssembler (MewtocolInterface interf) {

            onInterface = interf;

        }

        internal List<BaseRegister> Assemble (RBuild rBuildData) {

            List<BaseRegister> generatedInstances = new List<BaseRegister>();       

            foreach (var data in rBuildData.unfinishedList) {

                //parse all others where the type is known
                Type registerClassType = data.dotnetVarType.GetDefaultRegisterHoldingType();

                BaseRegister generatedInstance = null;

                if (data.dotnetVarType.IsEnum) {

                    //-------------------------------------------
                    //as numeric register with enum target

                    var underlying = Enum.GetUnderlyingType(data.dotnetVarType);
                    var enuSize = Marshal.SizeOf(underlying);

                    if (enuSize > 4)
                        throw new NotSupportedException("Enums not based on 16 or 32 bit numbers are not supported");

                    Type myParameterizedSomeClass = typeof(NumberRegister<>).MakeGenericType(data.dotnetVarType);
                    ConstructorInfo constr = myParameterizedSomeClass.GetConstructor(new Type[] { typeof(uint), typeof(string) });

                    var parameters = new object[] { data.memAddress, data.name };
                    var instance = (BaseRegister)constr.Invoke(parameters);
                    
                    generatedInstance = instance;

                } else if (registerClassType.IsGenericType) {

                    //-------------------------------------------
                    //as numeric register

                    //create a new bregister instance
                    var flags = BindingFlags.Public | BindingFlags.Instance;

                    //int _adress, Type _enumType = null, string _name = null
                    var parameters = new object[] { data.memAddress, data.name };
                    var instance = (BaseRegister)Activator.CreateInstance(registerClassType, flags, null, parameters, null);
                    instance.pollLevel = data.pollLevel;

                    generatedInstance = instance;

                } else if (registerClassType == typeof(BytesRegister) && data.byteSize != null) {

                    //-------------------------------------------
                    //as byte range register

                    BytesRegister instance = new BytesRegister(data.memAddress, (uint)data.byteSize, data.name);

                    generatedInstance = instance;

                } else if (registerClassType == typeof(BytesRegister) && data.bitSize != null) {

                    //-------------------------------------------
                    //as bit range register

                    BytesRegister instance = new BytesRegister(data.memAddress, (ushort)data.bitSize, data.name);

                    generatedInstance = instance;

                } else if (registerClassType == typeof(StringRegister)) {

                    //-------------------------------------------
                    //as byte range register
                    var instance = (BaseRegister)new StringRegister(data.memAddress, data.name);

                    generatedInstance = instance;

                } else if (data.regType.IsBoolean()) {

                    //-------------------------------------------
                    //as boolean register

                    var io = (IOType)(int)data.regType;
                    var spAddr = data.specialAddress;
                    var areaAddr = data.memAddress;

                    var instance = new BoolRegister(io, spAddr, areaAddr, data.name);

                    generatedInstance = instance;

                }

                //finalize set for every

                if(generatedInstance == null)
                    throw new MewtocolException("Failed to build register");

                if (collectionTarget != null)
                    generatedInstance.WithRegisterCollection(collectionTarget);

                generatedInstance.attachedInterface = onInterface;

                generatedInstance.pollLevel = data.pollLevel;

                generatedInstances.Add(generatedInstance);

            }

            return generatedInstances;

        } 

    }

}
