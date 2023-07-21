using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MewtocolNet {

    /// <summary>
    /// Holds various informations about the PLC
    /// </summary>
    public class PLCInfo : INotifyPropertyChanged {

        private PlcType typeCode;
        private string typeName;
        private OPMode operationMode;
        private HWInformation hardwareInformation;
        private string selfDiagnosticError;
       
        /// <summary>
        /// The type of the PLC named by Panasonic
        /// </summary>
        public PlcType TypeCode {
            get => typeCode;
            internal set {
                typeCode = value;
                OnPropChange();
                //update name
                typeName = typeCode.ToName();
                OnPropChange(nameof(TypeName));
            }
        }

        /// <summary>
        /// The full qualified name of the PLC 
        /// </summary>
        public string TypeName => typeName;

        /// <summary>
        /// Program capacity in 1K steps
        /// </summary>
        public float ProgramCapacity { get; internal set; }

        /// <summary>
        /// Version of the cpu
        /// </summary>
        public string CpuVersion { get; internal set; }

        /// <summary>
        /// Contains information about the PLCs operation modes as flags
        /// </summary>
        public OPMode OperationMode { 
            get => operationMode; 
            internal set {
                operationMode = value;
                OnPropChange();
                OnPropChange(nameof(IsRunMode));
            }
        }

        /// <summary>
        /// Hardware information flags about the PLC
        /// </summary>
        public HWInformation HardwareInformation { 
            get => hardwareInformation; 
            internal set {
                hardwareInformation = value;
                OnPropChange();

            }
        }

        /// <summary>
        /// Current error code of the PLC
        /// </summary>
        public string SelfDiagnosticError { 
            get => selfDiagnosticError; 
            internal set {
                selfDiagnosticError = value;
                OnPropChange();
            }
        }

        /// <summary>
        /// Quickcheck for the runmode flag
        /// </summary>
        public bool IsRunMode => OperationMode.HasFlag(OPMode.RunMode);

        public event PropertyChangedEventHandler PropertyChanged;

        internal bool TryExtendFromEXRT(string msg) {

            var regexEXRT = new Regex(@"\%EE\$EX00RT00(?<icnt>..)(?<mc>..)..(?<cap>..)(?<op>..)..(?<flg>..)(?<sdiag>....)(?<ver>..)(?<hwif>..)(?<nprog>.)(?<progsz>....)(?<hdsz>....)(?<sysregsz>....).*", RegexOptions.IgnoreCase);
            var match = regexEXRT.Match(msg);
            if (match.Success) {

                byte typeCodeByte = byte.Parse(match.Groups["mc"].Value, NumberStyles.HexNumber);
                var overWriteBytes = BitConverter.GetBytes((int)this.TypeCode);
                overWriteBytes[0] = typeCodeByte;

                this.TypeCode = (PlcType)BitConverter.ToInt32(overWriteBytes, 0);
                this.CpuVersion = match.Groups["ver"].Value.Insert(1, ".");
                this.HardwareInformation = (HWInformation)byte.Parse(match.Groups["hwif"].Value, NumberStyles.HexNumber);

                return true;

            }

            return false;

        }

        internal static bool TryFromRT(string msg, out PLCInfo inf) {

            var regexRT = new Regex(@"\%EE\$RT(?<cputype>..)(?<cpuver>..)(?<cap>..)(?<op>..)..(?<flg>..)(?<sdiag>....).*", RegexOptions.IgnoreCase);
            var match = regexRT.Match(msg);
            if (match.Success) {

                byte typeCodeByte = byte.Parse(match.Groups["cputype"].Value, NumberStyles.HexNumber);
                byte capacity = byte.Parse(match.Groups["cap"].Value, NumberStyles.Number);
                var typeCodeFull = (PlcType)BitConverter.ToInt32(new byte[] { typeCodeByte, capacity, 0, 0}, 0);

                float definedProgCapacity = 0;
                var composedNow = typeCodeFull.ToNameDecompose();

                if (composedNow != null) {

                    //already recognized the type code, use the capacity value encoded in the enum
                    definedProgCapacity = composedNow.Size;

                } else {

                    definedProgCapacity = int.Parse(match.Groups["cap"].Value);
                
                }

                inf = new PLCInfo {
                    TypeCode = typeCodeFull,
                    CpuVersion = match.Groups["cpuver"].Value.Insert(1, "."),
                    ProgramCapacity = definedProgCapacity,
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
        public static bool operator ==(PLCInfo c1, PLCInfo c2) {
            return c1.Equals(c2);
        }

        /// <inheritdoc/>
        public static bool operator !=(PLCInfo c1, PLCInfo c2) {
            return !c1.Equals(c2);
        }

        public override string ToString() {

            return $"{TypeName}, OP: {OperationMode}";

        }

        public override bool Equals(object obj) {

            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            } else {
                return (PLCInfo)obj == this;
            }

        }

        public override int GetHashCode() => GetHashCode();

        private protected void OnPropChange([CallerMemberName] string propertyName = null) {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

    }

}