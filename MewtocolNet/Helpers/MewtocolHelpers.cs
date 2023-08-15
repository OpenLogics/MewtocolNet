using MewtocolNet.Documentation;
using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace MewtocolNet {

    /// <summary>
    /// Contains helper methods
    /// </summary>
    public static class MewtocolHelpers {

        #region Async extensions

        internal static Task WhenCanceled(this CancellationToken cancellationToken) {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        #endregion

        #region Byte and string operation helpers

        public static T SetFlag<T>(this Enum value, T flag, bool set) {

            Type underlyingType = Enum.GetUnderlyingType(value.GetType());

            dynamic valueAsInt = Convert.ChangeType(value, underlyingType);
            dynamic flagAsInt = Convert.ChangeType(flag, underlyingType);

            if (set) {
                valueAsInt |= flagAsInt;
            } else {
                valueAsInt &= ~flagAsInt;
            }

            return (T)valueAsInt;
        
        }

        public static int DetermineTypeByteIntialSize(this Type type) {

            //enums can only be of numeric types
            if (type.IsEnum) return Marshal.SizeOf(Enum.GetUnderlyingType(type));

            //strings get always set with 4 bytes because the first 4 bytes contain the length
            if (type == typeof(string)) return 4;
            if (type == typeof(TimeSpan)) return 4;
            if (type == typeof(DateTime)) return 4;

            if (type.Namespace.StartsWith("System")) return Marshal.SizeOf(type);

            if (typeof(MewtocolExtTypeInit1Word).IsAssignableFrom(type)) return 2;
            if (typeof(MewtocolExtTypeInit2Word).IsAssignableFrom(type)) return 4;
            
            throw new Exception("Type not supported");

        }

        /// <summary>
        /// Searches a byte array for a pattern
        /// </summary>
        /// <param name="src"></param>
        /// <param name="pattern"></param>
        /// <returns>The start index of the found pattern or -1</returns>
        public static int SearchBytePattern(this byte[] src, byte[] pattern) {

            int maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++) {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--) {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;

        }

        /// <summary>
        /// Converts a string (after converting to upper case) to ascii bytes 
        /// </summary>
        internal static byte[] BytesFromHexASCIIString(this string _str) {

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] bytes = ascii.GetBytes(_str.ToUpper());
            return bytes;

        }

        /// <summary>
        /// Parses a return message as RCS single bit
        /// </summary>
        internal static bool? ParseRCSingleBit(this string _onString) {

            _onString = _onString.Replace("\r", "");

            var res = new Regex(@"\%([0-9a-fA-F]{2})\$RC(.)").Match(_onString);
            if (res.Success) {
                string val = res.Groups[2].Value;
                return val == "1";
            }
            return null;

        }

        /// <summary>
        /// Parses a return message as RCS multiple bits
        /// </summary>
        internal static BitArray ParseRCMultiBit(this string _onString) {

            _onString = _onString.Replace("\r", "");

            var res = new Regex(@"\%([0-9a-fA-F]{2})\$RC(?<bits>(?:0|1){0,8})(..)").Match(_onString);
            if (res.Success) {

                string val = res.Groups["bits"].Value;

                return new BitArray(val.Select(c => c == '1').ToArray());

            }
            return null;

        }

        /// <summary>
        /// Parses a return string from the PLC as a raw byte array <br/>
        /// Example:
        /// <code>
        ///       ↓Start ↓end
        /// %01$RD0100020010\r
        /// </code>
        /// This will return the byte array:
        /// <code>
        /// [0x01, 0x00, 0x02, 0x00]
        /// </code>
        /// </summary>
        /// <param name="_onString"></param>
        /// <returns>A <see cref="T:byte[]"/> or null of failed</returns>
        internal static byte[] ParseResponseStringAsBytes(this string _onString) {

            _onString = _onString.Replace("\r", "");

            var res = new Regex(@"(?:\%|\<)([0-9a-fA-F]{2})\$(?:RD|RP|RC)(?<data>.*)(?<csum>..)").Match(_onString);
            if (res.Success) {

                string val = res.Groups["data"].Value;
                var parts = val.SplitInParts(2).ToArray();
                var bytes = new byte[parts.Length];

                for (int i = 0; i < bytes.Length; i++) {

                    bytes[i] = byte.Parse(parts[i], NumberStyles.HexNumber);

                }

                return bytes;
            }
            return null;

        }

        /// <summary>
        /// Splits a string into even parts
        /// </summary>
        internal static IEnumerable<string> SplitInParts(this string s, int partLength) {

            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));

        }

        /// <summary>
        /// Splits a string by uppercase words
        /// </summary>
        internal static IEnumerable<string> SplitByAlternatingCase(this string str) {

            var words = new List<string>();
            var result = new StringBuilder();

            for (int i = 0; i < str.Length; i++) {

                char lastCh = str[Math.Max(0, i - 1)];
                char ch = str[i];

                if (char.IsUpper(ch) && char.IsLower(lastCh) && result.Length > 0) {
                    words.Add(result.ToString().Trim());
                    result.Clear();
                }

                result.Append(ch);

            }

            if (!string.IsNullOrEmpty(result.ToString()))
                words.Add(result.ToString().Trim());

            return words;

        }

        /// <summary>
        /// Splits a string by uppercase words and joins them with the given seperator
        /// </summary>
        internal static string JoinSplitByUpperCase(this string str, string seperator = " ") => string.Join(seperator, str.SplitByAlternatingCase());

        internal static string Ellipsis(this string str, int maxLength) {

            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;

            return  $"{str.Substring(0, maxLength - 3)}...";
        
        }

        internal static string SanitizeLinebreakFormatting (this string str) {

            str = str.Replace("\r", "").Replace("\n", "").Trim();

            return Regex.Replace(str, @"\s+", " ");

        }

        internal static string SanitizeBracketFormatting(this string str) {

            return str.Replace("(", "").Replace(")", "").Trim();

        }

        /// <summary>
        /// Converts a hex string (AB01C1) to a byte array
        /// </summary>
        internal static byte[] HexStringToByteArray(this string hex) {
            if (hex == null) return null;
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        /// <summary>
        /// Converts a byte array to a hexadecimal string
        /// </summary>
        /// <param name="seperator">Seperator between the hex numbers</param>
        /// <param name="arr">The byte array</param>
        public static string ToHexString(this byte[] arr, string seperator = "") {

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < arr.Length; i++) {
                byte b = arr[i];
                sb.Append(b.ToString("X2"));
                if (i < arr.Length - 1) sb.Append(seperator);
            }

            return sb.ToString();

        }

        /// <summary>
        /// Switches byte order from mixed to big endian
        /// </summary>
        internal static byte[] BigToMixedEndian(this byte[] arr) {

            List<byte> oldBL = new List<byte>(arr);

            List<byte> tempL = new List<byte>();

            //make the input list even
            if (arr.Length % 2 != 0)
                oldBL.Add((byte)0);

            for (int i = 0; i < oldBL.Count; i += 2) {
                byte firstByte = oldBL[i];
                byte lastByte = oldBL[i + 1];
                tempL.Add(lastByte);
                tempL.Add(firstByte);

            }

            return tempL.ToArray();

        }

        #endregion

        #region Comparerers

        /// <summary>
        /// Checks if the register type is boolean
        /// </summary>
        internal static bool IsBoolean(this RegisterPrefix type) {

            return type == RegisterPrefix.X || type == RegisterPrefix.Y || type == RegisterPrefix.R;

        }

        /// <summary>
        /// Checks if the register type numeric
        /// </summary>
        internal static bool IsNumericDTDDT(this RegisterPrefix type) {

            return type == RegisterPrefix.DT || type == RegisterPrefix.DDT;

        }

        /// <summary>
        /// Checks if the register type is an physical in or output of the plc
        /// </summary>
        internal static bool IsPhysicalInOutType(this RegisterPrefix type) {

            return type == RegisterPrefix.X || type == RegisterPrefix.Y;

        }

        internal static bool CompareIsDuplicate(this Register reg1, Register compare) {

            bool valCompare = reg1.RegisterType == compare.RegisterType &&
                              reg1.MemoryAddress == compare.MemoryAddress &&
                              reg1.GetRegisterAddressLen() == compare.GetRegisterAddressLen() &&
                              reg1.GetSpecialAddress() == compare.GetSpecialAddress();

            return valCompare;

        }

        internal static bool CompareIsDuplicateNonCast(this Register toInsert, Register compare, List<Type> allowOverlappingTypes) {

            foreach (var type in allowOverlappingTypes) {

                if (toInsert.GetType() == type) return false;

            }

            bool valCompare = toInsert.GetType() != compare.GetType() &&
                              toInsert.MemoryAddress == compare.MemoryAddress &&
                              toInsert.GetRegisterAddressLen() == compare.GetRegisterAddressLen() &&
                              toInsert.GetSpecialAddress() == compare.GetSpecialAddress();

            return valCompare;

        }

        internal static bool CompareIsNameDuplicate(this Register reg1, Register compare) {

            return (reg1.Name != null || compare.Name != null) && reg1.Name == compare.Name;

        }

        #endregion

        #region PLC Type Enum Parsing

        /// <summary>
        /// Gets synonim names for a plc type enum
        /// </summary>
        /// <returns>All or just one of there are no synonims for the same <see cref="PlcType"/></returns>
        public static string[] GetSynonims (this PlcType plcType) {

            return Enum.GetNames(typeof(PlcType)).Where(n => Enum.Parse(typeof(PlcType), n).Equals(plcType)).ToArray();

        }

        /// <summary>
        /// Converts the enum to a plc name string
        /// </summary>
        public static string ToName(this PlcType plcT) {

            if (plcT == 0) return "Unknown";

            if(!Enum.IsDefined(typeof(PlcType), plcT)) return "Unknown";

            return ParsedPlcName.PlcDeconstruct(plcT.ToString()).ToString();

        }

        /// <summary>
        /// Converts the enum to a decomposed <see cref="ParsedPlcName"/> struct
        /// </summary>
        public static ParsedPlcName ToNameDecompose(this PlcType plcT) {

            if ((int)plcT == 0)
                throw new NotSupportedException("No plc type found");

            if (!Enum.IsDefined(typeof(PlcType), plcT)) return null;

            return ParsedPlcName.PlcDeconstruct(plcT.ToString());

        }

        internal static ParsedPlcName ToNameDecompose(this string plcEnumString) {

            return ParsedPlcName.PlcDeconstruct(plcEnumString);

        }

        /// <summary>
        /// Checks if the PLC type is discontinued
        /// </summary>
        public static bool IsDiscontinued(this PlcType plcT) {

            var memberInfos = plcT.GetType().GetMember(plcT.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == plcT.GetType());
            var valueAttributes = enumValueMemberInfo?.GetCustomAttributes(typeof(PlcLegacyAttribute), false);
            if (valueAttributes != null) {
                var found = valueAttributes.FirstOrDefault(x => x.GetType() == typeof(PlcLegacyAttribute));
                if (found != null) return true;
            }

            return false;

        }

        #if DEBUG

        internal static bool WasTestedLive(this PlcType plcT) {

            var memberInfos = plcT.GetType().GetMember(plcT.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == plcT.GetType());
            var valueAttributes = enumValueMemberInfo?.GetCustomAttributes(typeof(PlcCodeTestedAttribute), false);
            if (valueAttributes != null) {
                var found = valueAttributes.FirstOrDefault(x => x.GetType() == typeof(PlcCodeTestedAttribute));
                if (found != null) return true;
            }

            return false;

        }

        internal static bool IsEXRTPLC(this PlcType plcT) {

            var memberInfos = plcT.GetType().GetMember(plcT.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == plcT.GetType());
            var valueAttributes = enumValueMemberInfo?.GetCustomAttributes(typeof(PlcEXRTAttribute), false);
            if (valueAttributes != null) {
                var found = valueAttributes.FirstOrDefault(x => x.GetType() == typeof(PlcEXRTAttribute));
                if (found != null) return true;
            }

            return false;

        }

        #endif

        #endregion

        #region Mapping

        /// <summary>
        /// Maps the source object to target object.
        /// </summary>
        /// <typeparam name="T">Type of target object.</typeparam>
        /// <typeparam name="TU">Type of source object.</typeparam>
        /// <param name="target">Target object.</param>
        /// <param name="source">Source object.</param>
        /// <returns>Updated target object.</returns>
        internal static T Map<T, TU>(this T target, TU source) {

            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var tprops = target.GetType().GetProperties();

            tprops.Where(x => x.CanWrite == true).ToList().ForEach(prop => {
                // check whether source object has the the property
                var sp = source.GetType().GetProperty(prop.Name);
                if (sp != null) {
                    // if yes, copy the value to the matching property
                    var value = sp.GetValue(source, null);
                    target.GetType().GetProperty(prop.Name).SetValue(target, value, null);
                }
            });

            var tfields = target.GetType().GetFields(flags);
            tfields.ToList().ForEach(field => {

                var sp = source.GetType().GetField(field.Name, flags);

                if (sp != null) {
                    // if yes, copy the value to the matching property
                    var value = sp.GetValue(source);
                    target.GetType().GetField(field.Name, flags).SetValue(target, value);
                }

            });

            return target;
        }

        #endregion


    }

}