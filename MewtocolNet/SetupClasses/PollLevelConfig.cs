using System;
using System.Diagnostics;

namespace MewtocolNet.SetupClasses {

    internal class PollLevelConfig {

        internal bool skipsAll;

        internal bool skipAllButFirst;

        internal TimeSpan? delay;

        internal int? skipNth;

        internal Stopwatch timeFromLastRead;

    }

}
