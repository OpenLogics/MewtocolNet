using System.Reflection;
using System.Text;

namespace MewtocolNet.RegisterAttributes {

    internal class RegisterPropTarget {

        //propinfo of the bound property
        internal PropertyInfo BoundProperty;

        internal RegisterAttribute PropertyAttribute;

        internal RegisterCollection ContainedCollection;

        public override string ToString() {

            var sb = new StringBuilder();
            sb.Append($"{BoundProperty}");
            return sb.ToString();

        }

    }

}
