using System;

namespace MewtocolNet.RegisterAttributes {
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class BitRegisterAttribute : RegisterAttribute {

        internal int bitIndex;

        /// <summary>
        /// Builds automatic data transfer between the property below this and 
        /// the plc register
        /// </summary>
        /// <param name="mewAddress">The FP-Address (DT, DDT, R, X, Y..)</param>
        /// <param name="plcTypeDef">The type definition from the PLC (STRING[n], ARRAY [0..2] OF ...)</param>
        public BitRegisterAttribute(string mewAddress, byte bitIndex) : base(mewAddress, null) {

            this.bitIndex = bitIndex;

        }

    }

}
