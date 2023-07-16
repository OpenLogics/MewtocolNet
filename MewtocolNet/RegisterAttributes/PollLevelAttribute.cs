using System;

namespace MewtocolNet.RegisterAttributes {

    /// <summary>
    /// Defines the poll level of the register
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PollLevelAttribute : Attribute {

        internal int pollLevel;

        public PollLevelAttribute(int level) {

            pollLevel = level;

        }

    }

}
