using System.Reflection;
using System.Text;

namespace MewtocolNet.RegisterAttributes {

    internal class RegisterPropTarget {

        //propinfo of the bound property
        internal PropertyInfo BoundProperty;

        //general number of bits or bytes to read back to the prop
        internal int? LinkLength;

        public override string ToString() {

            var sb = new StringBuilder();
            sb.Append($"{BoundProperty}");
            if(LinkLength != null) sb.Append($" -Len: {LinkLength}");

            return sb.ToString();

        }

    }

}
