using MewtocolNet.Registers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {

    public class WRArea : AreaBase, IMemoryArea {

        internal WRArea(MewtocolInterface mewIf) : base(mewIf) { }

        public override string ToString() => $"DT{AddressStart}-{AddressEnd}";

        public override string GetName() => $"{ToString()} ({managedRegisters.Count} Registers)";

    }

}
