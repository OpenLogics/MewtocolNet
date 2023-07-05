using MewtocolNet.DocAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MewtocolNet {

    /// <summary>
    /// Contains helper methods
    /// </summary>
    public static class MewtocolHelpers {

        #region Value PLC Humanizers

        /// <summary>
        /// Gets the TimeSpan as a PLC representation string fe.
        /// <code>
        /// T#1h10m30s20ms
        /// </code>
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns></returns>
        public static string AsPLCTime (this TimeSpan timespan) {

            if (timespan == null || timespan == TimeSpan.Zero)
                return $"T#0s";

            StringBuilder sb = new StringBuilder("T#");

            int millis = timespan.Milliseconds;
            int seconds = timespan.Seconds;
            int minutes = timespan.Minutes;
            int hours = timespan.Hours;

            if (hours > 0) sb.Append($"{hours}h");
            if (minutes > 0) sb.Append($"{minutes}m");
            if (seconds > 0) sb.Append($"{seconds}s");
            if (millis > 0) sb.Append($"{millis}ms");

            return sb.ToString();

        }

        /// <summary>
        /// Turns a bit array into a 0 and 1 string
        /// </summary>
        public static string ToBitString(this BitArray arr) {

            var bits = new bool[arr.Length];
            arr.CopyTo(bits, 0);
            return string.Join("", bits.Select(x => x ? "1" : "0"));

        }

        #endregion

        #region Byte and string operation helpers

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
        /// Builds the BCC / Checksum for the mewtocol command
        /// </summary>
        /// <param name="asciiArr">The mewtocol command (%01#RCS0001)</param>
        /// <returns>The mewtocol command with the appended checksum</returns>
        public static string BuildBCCFrame(this string asciiArr) {

            Encoding ae = Encoding.ASCII;
            byte[] b = ae.GetBytes(asciiArr);
            byte xorTotalByte = 0;
            for (int i = 0; i < b.Length; i++)
                xorTotalByte ^= b[i];
            return asciiArr.Insert(asciiArr.Length, xorTotalByte.ToString("X2"));

        }

        /// <summary>
        /// Parses the byte string from a incoming RD message
        /// </summary>
        internal static string ParseDTByteString(this string _onString, int _blockSize = 4) {

            if (_onString == null)
                return null;

            var res = new Regex(@"\%([0-9]{2})\$RD(.{" + _blockSize + "})").Match(_onString);
            if (res.Success) {
                string val = res.Groups[2].Value;
                return val;
            }
            return null;

        }

        /// <summary>
        /// Parses a return message as RCS single bit
        /// </summary>
        internal static bool? ParseRCSingleBit(this string _onString) {

            _onString = _onString.Replace("\r", "");

            var res = new Regex(@"\%([0-9]{2})\$RC(.)").Match(_onString);
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

            var res = new Regex(@"\%([0-9]{2})\$RC(?<bits>(?:0|1){0,8})(..)").Match(_onString);
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
        internal static byte[] ParseDTRawStringAsBytes (this string _onString) {

            var res = new Regex(@"\%([0-9]{2})\$RD(?<data>.*)(?<csum>..)..").Match(_onString);
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
        /// Converts a hex string (AB01C1) to a byte array
        /// </summary>
        internal static byte[] HexStringToByteArray (this string hex) {
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
        public static string ToHexString (this byte[] arr, string seperator = "") {

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < arr.Length; i++) {
                byte b = arr[i];
                sb.Append(b.ToString("X2"));
                if(i < arr.Length - 1) sb.Append(seperator);
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
        internal static bool IsBoolean (this RegisterType type) {

            return type == RegisterType.X || type == RegisterType.Y || type == RegisterType.R;

        }

        /// <summary>
        /// Checks if the register type numeric
        /// </summary>
        internal static bool IsNumericDTDDT (this RegisterType type) {

            return type == RegisterType.DT || type == RegisterType.DDT;

        }

        /// <summary>
        /// Checks if the register type is an physical in or output of the plc
        /// </summary>
        internal static bool IsPhysicalInOutType(this RegisterType type) {

            return type == RegisterType.X || type == RegisterType.Y;

        }

        internal static bool CompareIsDuplicate (this IRegisterInternal reg1, IRegisterInternal compare) {

            bool valCompare = reg1.RegisterType == compare.RegisterType &&
                              reg1.MemoryAddress == compare.MemoryAddress && 
                              reg1.GetSpecialAddress() == compare.GetSpecialAddress();

            return valCompare;

        }

        internal static bool CompareIsNameDuplicate(this IRegisterInternal reg1, IRegisterInternal compare) {

            return ( reg1.Name != null || compare.Name != null) && reg1.Name == compare.Name;

        }

        #endregion

        #region PLC Type Enum Parsing

        /// <summary>
        /// Converts the enum to a plc name string
        /// </summary>
        public static string ToName(this PlcType plcT) {

            return string.Join(" or ", ParsedPlcName.LegacyPlcDeconstruct(plcT).Select(x => x.WholeName));

        }

        /// <summary>
        /// Converts the enum to a decomposed <see cref="ParsedPlcName"/> struct
        /// </summary>
        public static ParsedPlcName[] ToNameDecompose (this PlcType legacyT) {

            return ParsedPlcName.LegacyPlcDeconstruct(legacyT);

        }

        /// <summary>
        /// Checks if the PLC type is discontinued
        /// </summary>
        public static bool IsDiscontinued (this PlcType plcT) {

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

        internal static bool WasTestedLive (this PlcType plcT) {

            var memberInfos = plcT.GetType().GetMember(plcT.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == plcT.GetType());
            var valueAttributes = enumValueMemberInfo?.GetCustomAttributes(typeof(PlcCodeTestedAttribute), false);
            if (valueAttributes != null) {
                var found = valueAttributes.FirstOrDefault(x => x.GetType() == typeof(PlcCodeTestedAttribute));
                if (found != null) return true;
            }

            return false;

        }

        internal static bool IsEXRTPLC (this PlcType plcT) {

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

    }

}