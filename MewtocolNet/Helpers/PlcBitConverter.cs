using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MewtocolNet.Helpers {

    internal static class PlcBitConverter {

        internal static string ToVersionNumber(IEnumerable<byte> inBytes, int startIndex = 0) {

            return string.Join(".", inBytes.Skip(startIndex).Take(4).Reverse().Select(x => x.ToString()));

        }

        internal static DateTime ToDateTime(IEnumerable<byte> inBytes, int startIndex = 0) {

            var offDate = new DateTime(2001, 01, 01);

            var secondsOff = BitConverter.ToUInt32(inBytes.ToArray(), startIndex);

            return offDate + TimeSpan.FromSeconds(secondsOff);

        }

    }

}
