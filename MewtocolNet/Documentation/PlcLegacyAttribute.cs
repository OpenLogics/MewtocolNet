using System;

namespace MewtocolNet.Documentation {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal class PlcLegacyAttribute : Attribute {

        public PlcLegacyAttribute() { }

    }

}
