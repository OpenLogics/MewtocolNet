using System;

namespace MewtocolNet {

    /// <summary>
    /// Contains information about the plc and its cpu
    /// </summary>
    public struct CpuInfo {

        /// <summary>
        /// The cpu type of the plc
        /// </summary>
        public CpuType Cputype { get; set; }

        /// <summary>
        /// Program capacity in 1K steps
        /// </summary>
        public int ProgramCapacity { get; set; }

        /// <summary>
        /// Version of the cpu
        /// </summary>
        public string CpuVersion { get; set; }

        internal static CpuInfo BuildFromHexString(string _cpuType, string _cpuVersion, string _progCapacity) {

            CpuInfo retInf = new CpuInfo();

            retInf.ProgramCapacity = Convert.ToInt32(_progCapacity);
            retInf.CpuVersion = _cpuVersion.Insert(1, ".");
            return retInf;

        }

    }

}