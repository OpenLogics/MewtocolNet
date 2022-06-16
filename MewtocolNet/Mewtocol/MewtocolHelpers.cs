using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MewtocolNet.Responses;

namespace MewtocolNet {
    public static class MewtocolHelpers {
        public static Byte[] ToHexASCIIBytes (this string _str) {
            ASCIIEncoding ascii = new ASCIIEncoding();
            Byte[] bytes = ascii.GetBytes(_str.ToUpper()); 
            return bytes;
        }

        public static string BuildBCCFrame (this string asciiArr) {
            Encoding ae = Encoding.ASCII;
            byte[] b = ae.GetBytes(asciiArr);
            byte xorTotalByte = 0;
            for(int i = 0; i < b.Length; i++)
            xorTotalByte ^= b[i];
            return asciiArr.Insert(asciiArr.Length, xorTotalByte.ToString("X2"));
        }

        public static byte[] ParseDTBytes (this string _onString ,int _blockSize = 4) {
            var res = new Regex(@"\%([0-9]{2})\$RD(.{"+_blockSize+"})").Match(_onString);
            if(res.Success) {
                string val = res.Groups[2].Value;
                return val.HexStringToByteArray();
            }
            return null;
        }

        public static string ParseDTByteString (this string _onString, int _blockSize = 4) {
            var res = new Regex(@"\%([0-9]{2})\$RD(.{" + _blockSize + "})").Match(_onString);
            if (res.Success) {
                string val = res.Groups[2].Value;
                return val;
            }
            return null;
        }

        public static string ParseDTString (this string _onString) {
            var res = new Regex(@"\%([0-9]{2})\$RD.{8}(.*)...").Match(_onString);
            if(res.Success) {
                string val = res.Groups[2].Value;
                return val.GetStringFromAsciiHex();
            }
            return null;
        }

        public static string BuildDTString (this string _inString, short _stringReservedSize) {
            StringBuilder sb = new StringBuilder();
            //06000600
            short stringSize = (short)_inString.Length;
            var sizeBytes = BitConverter.GetBytes(stringSize).ToHexString();
            var reservedSizeBytes = BitConverter.GetBytes(_stringReservedSize).ToHexString();
            //reserved string count bytes
            sb.Append(reservedSizeBytes);
            //string count actual bytes
            sb.Append(sizeBytes);
            //actual string content
            sb.Append(_inString.GetAsciiHexFromString().PadRight(_stringReservedSize * 2, '0'));

            return sb.ToString();
        }


        public static string GetStringFromAsciiHex (this string input) {
            if (input.Length % 2 != 0)
                throw new ArgumentException("input not a hex string");
            byte[] bytes = new byte[input.Length / 2];
            for (int i = 0; i < input.Length; i += 2) {
                String hex = input.Substring(i, 2);
                bytes[i/2] = Convert.ToByte(hex, 16);                
            }
            return Encoding.ASCII.GetString(bytes);
        }

        public static string GetAsciiHexFromString (this string input) {
            var bytes = new ASCIIEncoding().GetBytes(input);
            return bytes.ToHexString();
        }

        public static byte[] HexStringToByteArray(this string hex) {
            return Enumerable.Range(0, hex.Length)
                            .Where(x => x % 2 == 0)
                            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                            .ToArray();
        }

        public static string ToHexString (this byte[] arr) {
            StringBuilder sb = new StringBuilder();
            foreach (var b in arr) {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        public static string ToJsonString (this IEnumerable<IBoolContact> _contacts, bool formatPretty = false) {
            return JsonSerializer.Serialize(_contacts, new JsonSerializerOptions {
                WriteIndented = formatPretty,
            });
        }

        public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck) {
            while (toCheck != null && toCheck != typeof(object)) {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

    }

}