using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MewtocolNet {

    /// <summary>
    /// A structure containing the PLC name parsed
    /// </summary>
    public class ParsedPlcName {

        /// <summary>
        /// Whole name of the PLC
        /// </summary>
        public string WholeName { get; internal set; }

        /// <summary>
        /// The family group of the PLC
        /// </summary>
        public string Group { get; internal set; }

        /// <summary>
        /// The Memory size of the PLC
        /// </summary>
        public float Size { get; internal set; }

        /// <summary>
        /// The subtype strings of the plc
        /// </summary>
        public string[] SubTypes { get; internal set; }

        /// <summary>
        /// Typecode of the parsed string
        /// </summary>
        public int TypeCode { get; internal set; }  

        /// <summary>
        /// The encoded name, same as enum name
        /// </summary>
        public string EncodedName { get; internal set; }    

        /// <summary>
        /// True if the model is discontinued
        /// </summary>
        public bool IsDiscontinuedModel { get; internal set; }  

        internal bool WasTestedLive { get; set; }

        internal bool UsesEXRT { get; set; }

        /// <inheritdoc/>
        public override string ToString() => WholeName;

        internal static ParsedPlcName PlcDeconstruct(string wholeStr) {

            var reg = new Regex(@"(?<group>[A-Za-z0-9]*)_(?<size>[A-Za-z0-9]*)(?:__)?(?<additional>.*)");
            var match = reg.Match(wholeStr);

            if (match.Success) {

                string groupStr = SanitizePlcEncodedString(match.Groups["group"].Value);
                string sizeStr = SanitizePlcEncodedString(match.Groups["size"].Value);
                float sizeFl = float.Parse(sizeStr.Replace("k", ""), NumberStyles.Float, CultureInfo.InvariantCulture);
                string additionalStr = match.Groups["additional"].Value;
                string[] subTypes = additionalStr.Split('_').Select(x => SanitizePlcEncodedString(x)).ToArray();

                string wholeName = $"{groupStr} {sizeFl:0.##}k{(subTypes.Length > 0 ? " " : "")}{string.Join(",", subTypes)}";

                if (string.IsNullOrEmpty(subTypes[0]))
                    subTypes = Array.Empty<string>();

                int typeCode = 999;
                bool discontinued = false, exrt = false, tested = false;
                
                if(Enum.TryParse(wholeStr, out PlcType t)) {

                    typeCode = (int)t;
                    discontinued = t.IsDiscontinued();
                    exrt = t.IsEXRTPLC();
                    tested = t.WasTestedLive();

                }

                return new ParsedPlcName {
                    Group = groupStr,
                    Size = sizeFl,
                    SubTypes = subTypes,
                    WholeName = wholeName,
                    EncodedName = wholeStr,
                    TypeCode = typeCode,
                    IsDiscontinuedModel = discontinued,
                    UsesEXRT = exrt,
                    WasTestedLive = tested, 
                };

            } else {

                throw new FormatException($"The plc enum was not formatted correctly: {wholeStr}");

            }

        }

        private static string SanitizePlcEncodedString(string input) {

            input = input.Replace("d", "-");
            input = input.Replace("c", ".");
            input = input.Replace("s", "/");

            return input;

        }

    }

}
