using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MewtocolNet {
    
    /// <summary>
    /// A structure containing the PLC name parsed
    /// </summary>
    public struct ParsedPlcName {

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

        /// <inheritdoc/>
        public override string ToString() => WholeName;

        internal static ParsedPlcName[] PlcDeconstruct (PlcType plcT) {

            string wholeStr = plcT.ToString();

            var split = wholeStr.Replace("_OR_", "|").Split('|');
            var reg = new Regex(@"(?<group>[A-Za-z0-9]*)_(?<size>[A-Za-z0-9]*)(?:__)?(?<additional>.*)");

            var retList = new List<ParsedPlcName>();

            foreach (var item in split) {

                var match = reg.Match(item);

                if (match.Success) {

                    string groupStr = SanitizePlcEncodedString(match.Groups["group"].Value);
                    string sizeStr = SanitizePlcEncodedString(match.Groups["size"].Value);
                    float sizeFl = float.Parse(sizeStr.Replace("k", ""), NumberStyles.Float, CultureInfo.InvariantCulture);
                    string additionalStr = match.Groups["additional"].Value;
                    string[] subTypes = additionalStr.Split('_').Select(x => SanitizePlcEncodedString(x)).ToArray();

                    string wholeName = $"{groupStr} {sizeFl:0.##}k{(subTypes.Length > 1 ? " " : "")}{string.Join(",", subTypes)}";

                    if (string.IsNullOrEmpty(subTypes[0]))
                        subTypes = Array.Empty<string>();   

                    retList.Add(new ParsedPlcName {
                        Group = groupStr,
                        Size = sizeFl,
                        SubTypes = subTypes,
                        WholeName = wholeName,
                    });

                } else {

                    throw new FormatException($"The plc enum was not formatted correctly: {item}");

                }

            }

            return retList.ToArray();

        }

        private static string SanitizePlcEncodedString(string input) {

            input = input.Replace("d", "-");
            input = input.Replace("c", ".");
            input = input.Replace("s", "/");

            return input;

        }

    }

}
