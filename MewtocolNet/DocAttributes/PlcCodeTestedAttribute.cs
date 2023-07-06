using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.DocAttributes {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal class PlcCodeTestedAttribute : Attribute {

        public PlcCodeTestedAttribute() { }
        
    }

}