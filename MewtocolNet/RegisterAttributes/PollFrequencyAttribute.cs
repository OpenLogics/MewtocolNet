using System;

namespace MewtocolNet.RegisterAttributes {
    /// <summary>
    /// Defines the behavior of a register property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PollFrequencyAttribute : Attribute {

        internal int skipEachCycle;
        
        public PollFrequencyAttribute(int eachCycleN) {

            skipEachCycle = eachCycleN;

        }

    }

}
