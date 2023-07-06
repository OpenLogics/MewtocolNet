using System.Globalization;
using System.Text.RegularExpressions;

namespace MewtocolNet {

    /// <summary>
    /// Holds various informations about the PLC
    /// </summary>
    public struct PLCInfo {

        /// <summary>
        /// The type of the PLC named by Panasonic
        /// </summary>
        public PlcType TypeCode { get; private set; }

        /// <summary>
        /// Contains information about the PLCs operation modes as flags
        /// </summary>
        public OPMode OperationMode { get; private set; }

        /// <summary>
        /// Hardware information flags about the PLC
        /// </summary>
        public HWInformation HardwareInformation { get; private set; }  

        /// <summary>
        /// Program capacity in 1K steps
        /// </summary>
        public int ProgramCapacity { get; private set; }

        /// <summary>
        /// Version of the cpu
        /// </summary>
        public string CpuVersion { get; private set; }

        /// <summary>
        /// Current error code of the PLC
        /// </summary>
        public string SelfDiagnosticError { get; internal set; }

        /// <summary>
        /// Quickcheck for the runmode flag
        /// </summary>
        public bool IsRunMode => OperationMode.HasFlag(OPMode.RunMode);

        internal bool TryExtendFromEXRT (string msg) {

            var regexEXRT = new Regex(@"\%EE\$EX00RT00(?<icnt>..)(?<mc>..)..(?<cap>..)(?<op>..)..(?<flg>..)(?<sdiag>....)(?<ver>..)(?<hwif>..)(?<nprog>.)(?<progsz>....)(?<hdsz>....)(?<sysregsz>....).*", RegexOptions.IgnoreCase);
            var match = regexEXRT.Match(msg);      
            if(match.Success) {

                byte typeCodeByte = byte.Parse(match.Groups["mc"].Value, NumberStyles.HexNumber);

                this.TypeCode = (PlcType)typeCodeByte;
                this.CpuVersion = match.Groups["ver"].Value;
                this.HardwareInformation = (HWInformation)byte.Parse(match.Groups["hwif"].Value, NumberStyles.HexNumber);

                return true;

            }

            return false;

        }

        internal static bool TryFromRT (string msg, out PLCInfo inf) {

            var regexRT = new Regex(@"\%EE\$RT(?<cputype>..)(?<cpuver>..)(?<cap>..)(?<op>..)..(?<flg>..)(?<sdiag>....).*", RegexOptions.IgnoreCase);
            var match = regexRT.Match(msg);
            if (match.Success) {

                byte typeCodeByte = byte.Parse(match.Groups["cputype"].Value, NumberStyles.HexNumber);

                inf = new PLCInfo {
                    TypeCode = (PlcType)typeCodeByte,
                    CpuVersion = match.Groups["cpuver"].Value,
                    ProgramCapacity = int.Parse(match.Groups["cap"].Value),
                    SelfDiagnosticError = match.Groups["sdiag"].Value,
                    OperationMode = (OPMode)byte.Parse(match.Groups["op"].Value, NumberStyles.HexNumber),
                };

                return true;

            }

            inf = default(PLCInfo);
            return false;

        }

        /// <summary>
        /// Plc info when its not connected 
        /// </summary>
        public static PLCInfo None => new PLCInfo() {

            SelfDiagnosticError = "",
            CpuVersion = "",
            HardwareInformation = 0,
            OperationMode = 0,
            ProgramCapacity = 0,
            TypeCode = 0,
        
        };

        /// <inheritdoc/>
        public static bool operator == (PLCInfo c1, PLCInfo c2) {
            return c1.Equals(c2);
        }

        /// <inheritdoc/>
        public static bool operator != (PLCInfo c1, PLCInfo c2) {
            return !c1.Equals(c2);
        }

        public override string ToString() {

            return $"{TypeCode.ToName()}, OP: {OperationMode}";

        }

    }

}