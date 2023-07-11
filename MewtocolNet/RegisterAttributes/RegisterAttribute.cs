using MewtocolNet.RegisterBuilding;
using System;

namespace MewtocolNet.RegisterAttributes {

    /// <summary>
    /// Defines the behavior of a register property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegisterAttribute : Attribute {

        internal int AssignedBitIndex = -1;

        internal string MewAddress = null;

        public RegisterAttribute(string mewAddress) {

            MewAddress = mewAddress;    

        }

    }

}
