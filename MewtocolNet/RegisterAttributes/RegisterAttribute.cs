using System;

namespace MewtocolNet.RegisterAttributes {

    /// <summary>
    /// Defines the behavior of a register property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegisterAttribute : Attribute {

        internal string MewAddress = null;
        internal string TypeDef = null;

        /// <summary>
        /// Builds automatic data transfer between the property below this and 
        /// the plc register
        /// </summary>
        /// <param name="mewAddress">The FP-Address (DT, DDT, R, X, Y..)</param>
        /// <param name="plcTypeDef">The type definition from the PLC (STRING[n], ARRAY [0..2] OF ...)</param>
        public RegisterAttribute(string mewAddress, string plcTypeDef = null) {

            MewAddress = mewAddress;
            TypeDef = plcTypeDef;

        }

    }

}
