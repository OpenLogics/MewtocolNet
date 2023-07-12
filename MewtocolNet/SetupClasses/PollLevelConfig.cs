using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MewtocolNet.SetupClasses {

    internal class PollLevelConfig {

        internal TimeSpan? delay;

        internal int? skipNth;

        internal Stopwatch timeFromLastRead;

    }

}
