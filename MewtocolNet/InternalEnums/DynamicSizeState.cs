using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet {

    [Flags]
    internal enum DynamicSizeState {

        None = 0,
        DynamicallySized = 1,
        NeedsSizeUpdate = 2,
        WasSizeUpdated = 4,

    }

}
