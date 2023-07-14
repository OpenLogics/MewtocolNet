using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.Documentation {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal class PlcEXRTAttribute : Attribute {
    
        public PlcEXRTAttribute() {} 
        
    }

}
