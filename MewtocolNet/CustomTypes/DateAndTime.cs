using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MewtocolNet {

    /// <summary>
    /// A DateAndTime struct of 4 bytes represented as seconds from 2001-01-01 in the PLC<br/>
    /// This also works for the PLC type TIME_OF_DAY and DATE
    /// </summary>
    public struct DateAndTime : MewtocolExtTypeInit2Word {

        internal DateTime value;

        public DateAndTime(int year = 2001, int month = 1, int day = 1, int hour = 0, int minute = 0, int second = 0) {

            var minDate = MinDate;
            var maxDate = MaxDate;  

            if (year < 2001 || year > 2099) 
                throw new NotSupportedException("Year must be between 2001 and 2099");

            if (month < 1 || month > 12)
                throw new NotSupportedException("Month must be between 1 and 12");

            if (day < 1 || day > 32)
                throw new NotSupportedException("Day must be between 1 and 32");

            if (day < 1 || day > 32)
                throw new NotSupportedException("Month must be between 1 and 32");

            var dt = new DateTime(year, month, day, hour, minute, second);

            if (dt < minDate)
                throw new Exception($"The minimal DATE_AND_TIME repesentation is {minDate}");

            if (dt > maxDate)
                throw new Exception($"The maximal DATE_AND_TIME repesentation is {maxDate}");

            value = dt;

        }

        public static DateAndTime FromBytes(byte[] bytes) {

            var secondsFrom = BitConverter.ToUInt32(bytes, 0);

            return FromDateTime(MinDate + TimeSpan.FromSeconds(secondsFrom));

        }

        public static DateAndTime FromDateTime(DateTime time) => new DateAndTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);

        //operations

        public static TimeSpan operator -(DateAndTime a, DateAndTime b) => a.value - b.value;

        public static DateAndTime operator +(DateAndTime a, TimeSpan b) => FromDateTime(a.value + b);

        public static bool operator ==(DateAndTime a, DateAndTime b) => a.value == b.value;

        public static bool operator !=(DateAndTime a, DateAndTime b) => a.value != b.value;

        public override bool Equals(object obj) {

            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            } else {
                return (DateAndTime)obj == this;
            }

        }

        public override int GetHashCode() => value.GetHashCode();

        public byte[] ToByteArray() => BitConverter.GetBytes(SecondsSinceStart());

        public DateTime ToDateTime() => value;

        private uint SecondsSinceStart() => (uint)(value - MinDate).TotalSeconds;

        private static DateTime MinDate => new DateTime(2001, 01, 01, 0, 0, 0);

        private static DateTime MaxDate => new DateTime(2099, 12, 31, 23, 59, 59);

        //string ops

        public override string ToString() => $"DT#{value:yyyy-MM-dd-HH:mm:ss}";

    }

}
