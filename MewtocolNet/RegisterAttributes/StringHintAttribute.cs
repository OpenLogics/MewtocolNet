using System;

namespace MewtocolNet.RegisterAttributes {
    /// <summary>
    /// Defines a string size hint
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class StringHintAttribute : Attribute {

        internal int size;

        public StringHintAttribute(int size) {

            this.size = size;

        }

    }

}
