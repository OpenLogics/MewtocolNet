using System;

namespace MewtocolNet.Logging {
    [Flags]
    public enum LoggerTargets {

        None = 0,
        Console = 1,
        Trace = 2,

    }
}
