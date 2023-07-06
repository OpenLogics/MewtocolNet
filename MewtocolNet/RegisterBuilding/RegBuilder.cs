using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MewtocolNet.RegisterBuilding {

    /// <summary>
    /// Contains useful tools for register creation
    /// </summary>
    public class RegBuilder {

        internal MewtocolInterface forInterface = null;

        //methods to test the input string on
        private static List<Func<string, ParseResult>> parseMethods = new List<Func<string, ParseResult>>() {

            (x) => TryBuildBoolean(x),
            (x) => TryBuildNumericBased(x),

        };

        public static RegBuilder ForInterface (IPlc interf) {

            var rb = new RegBuilder();
            rb.forInterface = interf as MewtocolInterface;
            return rb;

        }

        public static RegBuilder ForInterface(IPlcSerial interf) {

            var rb = new RegBuilder();
            rb.forInterface = interf as MewtocolInterface;
            return rb;

        }

        public static RegBuilder Factory { get; private set; } = new RegBuilder();


        public RegisterBuilderStep FromPlcRegName (string plcAddrName, string name = null) {

            foreach (var method in parseMethods) {

                var res = method.Invoke(plcAddrName);

                if(res.state == ParseResultState.Success) {

                    if (!string.IsNullOrEmpty(name))
                        res.stepData.Name = name;

                    res.stepData.OriginalInput = plcAddrName;
                    res.stepData.forInterface = forInterface;
                    return res.stepData;

                } else if(res.state == ParseResultState.FailedHard) {

                    throw new Exception(res.hardFailReason);

                }

            }

            throw new Exception("Wrong input format");

        }

        //bool registers
        private static ParseResult TryBuildBoolean (string plcAddrName) {

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
            int areaAdd = 0;
            byte specialAdd = 0x0;

            //try cast the prefix
            if(!Enum.TryParse(prefix, out regType)) {
                
                return new ParseResult {
                    state = ParseResultState.FailedHard,
                    hardFailReason = $"Cannot parse '{plcAddrName}', the prefix is not allowed for boolean registers"
                };

            } 

            if(!string.IsNullOrEmpty(area) && !int.TryParse(area, out areaAdd) ) {

                return new ParseResult {
                    state = ParseResultState.FailedHard,
                    hardFailReason = $"Cannot parse '{plcAddrName}', the area address: '{area}' is wrong"
                };

            } 

            //special address not given
            if(string.IsNullOrEmpty(special) && !string.IsNullOrEmpty(area)) {

                var isAreaInt = int.TryParse(area, NumberStyles.Number, CultureInfo.InvariantCulture, out var areaInt);

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
                stepData = new RegisterBuilderStep ((RegisterType)(int)regType, areaAdd, specialAdd),
            };

        }

        // one to two word registers
        private static ParseResult TryBuildNumericBased (string plcAddrName) {

            var patternByte = new Regex(@"(?<prefix>DT|DDT)(?<area>[0-9]{1,5})");

            var match = patternByte.Match(plcAddrName);

            if (!match.Success)
                return new ParseResult {
                    state = ParseResultState.FailedSoft
                };


            string prefix = match.Groups["prefix"].Value;
            string area = match.Groups["area"].Value;

            RegisterType regType;
            int areaAdd = 0;

            //try cast the prefix
            if (!Enum.TryParse(prefix, out regType)) {

                return new ParseResult {
                    state = ParseResultState.FailedHard,
                    hardFailReason = $"Cannot parse '{plcAddrName}', the prefix is not allowed for numeric registers"
                };

            }

            if (!string.IsNullOrEmpty(area) && !int.TryParse(area, out areaAdd)) {

                return new ParseResult {
                    state = ParseResultState.FailedHard,
                    hardFailReason = $"Cannot parse '{plcAddrName}', the area address: '{area}' is wrong"
                };

            }

            return new ParseResult {
                state = ParseResultState.Success,
                stepData = new RegisterBuilderStep(regType, areaAdd),
            };

        }

    }

}
