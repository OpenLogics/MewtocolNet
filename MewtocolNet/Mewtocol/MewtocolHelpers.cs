using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using MewtocolNet.Registers;
using System.Collections;

namespace MewtocolNet {

    /// <summary>
    /// Contains helper methods
    /// </summary>
    public static class MewtocolHelpers {

        /// <summary>
        /// Turns a bit array into a 0 and 1 string
        /// </summary>
        public static string ToBitString (this BitArray arr) {

            var bits = new bool[arr.Length];
            arr.CopyTo(bits, 0);
            return string.Join("", bits.Select(x => x ? "1" : "0"));

        }

        internal static byte[] ToHexASCIIBytes (this string _str) {
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] bytes = ascii.GetBytes(_str.ToUpper()); 
            return bytes;
        }

        internal static string BuildBCCFrame (this string asciiArr) {
            Encoding ae = Encoding.ASCII;
            byte[] b = ae.GetBytes(asciiArr);
            byte xorTotalByte = 0;
            for(int i = 0; i < b.Length; i++)
            xorTotalByte ^= b[i];
            return asciiArr.Insert(asciiArr.Length, xorTotalByte.ToString("X2"));
        }

        internal static byte[] ParseDTBytes (this string _onString ,int _blockSize = 4) {
            var res = new Regex(@"\%([0-9]{2})\$RD(.{"+_blockSize+"})").Match(_onString);
            if(res.Success) {
                string val = res.Groups[2].Value;
                return val.HexStringToByteArray();
            }
            return null;
        }

        internal static string ParseDTByteString (this string _onString, int _blockSize = 4) {

            if (_onString == null)
                return null;

            var res = new Regex(@"\%([0-9]{2})\$RD(.{" + _blockSize + "})").Match(_onString);
            if (res.Success) {
                string val = res.Groups[2].Value;
                return val;
            }
            return null;

        }

        internal static bool? ParseRCSingleBit (this string _onString, int _blockSize = 4) {
            var res = new Regex(@"\%([0-9]{2})\$RC(.)").Match(_onString);
            if (res.Success) {
                string val = res.Groups[2].Value;
                return val == "1";
            }
            return null;
        }

        internal static string ParseDTString (this string _onString) {
            var res = new Regex(@"\%([0-9]{2})\$RD.{8}(.*)...").Match(_onString);
            if(res.Success) {
                string val = res.Groups[2].Value;
                return val.GetStringFromAsciiHex().Trim();
            }
            return null;
        }

        internal static string ReverseByteOrder (this string _onString) {

            if(_onString == null) return null;

            //split into 2 chars
            var stringBytes = _onString.SplitInParts(2).ToList();

            stringBytes.Reverse();

            return string.Join("", stringBytes);

        }

        internal static IEnumerable<String> SplitInParts (this string s, int partLength) {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

        internal static string BuildDTString (this string _inString, short _stringReservedSize) {

            StringBuilder sb = new StringBuilder();

            //clamp string lenght
            if (_inString.Length > _stringReservedSize) {
                _inString = _inString.Substring(0, _stringReservedSize);
            }

            //actual string content
            var hexstring = _inString.GetAsciiHexFromString();

            var sizeBytes = BitConverter.GetBytes((short)(hexstring.Length / 2)).ToHexString();

            if (hexstring.Length >= 2) {

                var remainderBytes = (hexstring.Length / 2) % 2;

                if (remainderBytes != 0) {
                    hexstring += "20";
                }

            }

            var reservedSizeBytes = BitConverter.GetBytes(_stringReservedSize).ToHexString();

            //reserved string count bytes
            sb.Append(reservedSizeBytes);
            //string count actual bytes
            sb.Append(sizeBytes);
            

            sb.Append(hexstring);

            return sb.ToString();
        }


        internal static string GetStringFromAsciiHex (this string input) {
            if (input.Length % 2 != 0)
                throw new ArgumentException("input not a hex string");
            byte[] bytes = new byte[input.Length / 2];
            for (int i = 0; i < input.Length; i += 2) {
                String hex = input.Substring(i, 2);
                bytes[i/2] = Convert.ToByte(hex, 16);                
            }
            return Encoding.ASCII.GetString(bytes);
        }

        internal static string GetAsciiHexFromString (this string input) {
            var bytes = new ASCIIEncoding().GetBytes(input);
            return bytes.ToHexString();
        }

        internal static byte[] HexStringToByteArray(this string hex) {
            return Enumerable.Range(0, hex.Length)
                            .Where(x => x % 2 == 0)
                            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                            .ToArray();
        }

        internal static string ToHexString (this byte[] arr) {
            StringBuilder sb = new StringBuilder();
            foreach (var b in arr) {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

    }

}