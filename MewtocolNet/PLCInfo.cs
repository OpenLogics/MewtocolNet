using System;
using System.Collections.Generic;
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

        private MewtocolInterface plc;

        private PlcType typeCode;
        private string typeName;
        private OPMode operationMode;
        private HWInformation hardwareInformation;
        private string selfDiagnosticError;
        private PlcMetadata metadata;
       
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

                var lastModeFlags = operationMode;

                operationMode = value;
                OnPropChange();
                OnPropChange(nameof(IsRunMode));
                OnPropChange(nameof(OperationModeTags));

                if (plc != null && plc.IsConnected && !plc.isConnectingStage && lastModeFlags != OPMode.None)
                    plc.InvokeModeChanged(lastModeFlags, value);

            }
        }

        /// <summary>
        /// A list of operation mode tags, derived from the OPMode flags
        /// </summary>
        public IEnumerable<string> OperationModeTags {
            get => OperationMode.ToString().Split(',').Select(x => x.JoinSplitByUpperCase().Trim());
        }

        /// <summary>
        /// Hardware information flags about the PLC
        /// </summary>
        public HWInformation HardwareInformation { 
            get => hardwareInformation; 
            internal set {
                hardwareInformation = value;
                OnPropChange();
                OnPropChange(nameof(HardwareInformationTags));
            }
        }

        /// <summary>
        /// A list of hardware info tags, derived from the HardwareInformation flags
        /// </summary>
        public IEnumerable<string> HardwareInformationTags {
            get => HardwareInformation.ToString().Split(',').Select(x => x.JoinSplitByUpperCase().Trim());
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

        /// <summary>
        /// Contains useful information about the PLC program and metadata
        /// </summary>
        public PlcMetadata Metadata {
            get => metadata;
            internal set {
                metadata = value;   
                OnPropChange(); 
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal PLCInfo (MewtocolInterface onInterface) {

            plc = onInterface;

        }

        internal bool TryExtendFromEXRT(string msg) {

            var regexEXRT = new Regex(@"\%..\$EX00RT00(?<icnt>..)(?<mc>..)..(?<cap>..)(?<op>..)..(?<flg>..)(?<sdiag>....)(?<ver>..)(?<hwif>..)(?<nprog>.)(?<csumpz>...)(?<psize>...).*", RegexOptions.IgnoreCase);
            var match = regexEXRT.Match(msg);
            if (match.Success) {

                //overwrite the typecode
                byte typeCodeByte = byte.Parse(match.Groups["mc"].Value, NumberStyles.HexNumber);
                var overWriteBytes = BitConverter.GetBytes((int)this.TypeCode);
                overWriteBytes[0] = typeCodeByte;

                //get the long (4 bytes) prog size 
                if (match.Groups["psize"]?.Value != null) {

                    var padded = match.Groups["psize"].Value.PadLeft(4, '0');

                    overWriteBytes[1] = byte.Parse(padded.Substring(2, 2), NumberStyles.HexNumber);
                    overWriteBytes[2] = byte.Parse(padded.Substring(0, 2), NumberStyles.HexNumber);

                }

                var tempTypeCode = BitConverter.ToUInt32(overWriteBytes, 0);

                if (Enum.IsDefined(typeof(PlcType), tempTypeCode)) {

                    this.TypeCode = (PlcType)tempTypeCode;
                
                }

                var cpuVerStr = match.Groups["ver"].Value;

                //overwrite the other vals that are also contained in EXRT
                this.CpuVersion = string.Join(".", cpuVerStr.Select(x => byte.Parse($"{x}", NumberStyles.HexNumber).ToString()));
                this.HardwareInformation = (HWInformation)byte.Parse(match.Groups["hwif"].Value, NumberStyles.HexNumber);

                return true;

            }

            return false;

        }

        internal static bool TryFromRT(string msg, MewtocolInterface onInterface, out PLCInfo inf) {

            var regexRT = new Regex(@"\%..\$RT(?<cputype>..)(?<cpuver>..)(?<cap>..)(?<op>..)..(?<flg>..)(?<sdiag>....).*", RegexOptions.IgnoreCase);
            var match = regexRT.Match(msg);
            if (match.Success) {

                byte typeCodeByte = byte.Parse(match.Groups["cputype"].Value, NumberStyles.HexNumber);
                byte capacity = byte.Parse(match.Groups["cap"].Value, NumberStyles.Number);
                var tempTypeCode = (PlcType)BitConverter.ToUInt32(new byte[] { typeCodeByte, capacity, 0, 0}, 0);

                float definedProgCapacity = 0;
                PlcType typeCodeFull;

                if (Enum.IsDefined(typeof(PlcType), tempTypeCode)) {

                    typeCodeFull = (PlcType)tempTypeCode;

                    var composedNow = typeCodeFull.ToNameDecompose();

                    if (composedNow != null) {

                        //already recognized the type code, use the capacity value encoded in the enum
                        definedProgCapacity = composedNow.Size;

                    }

                } else {

                    typeCodeFull = PlcType.Unknown;
                    definedProgCapacity = int.Parse(match.Groups["cap"].Value);

                }

                var cpuVerStr = match.Groups["cpuver"].Value;
                var cpuVer = string.Join(".", cpuVerStr.Select(x => byte.Parse($"{x}").ToString("X1")));

                inf = new PLCInfo (onInterface) {
                    TypeCode = typeCodeFull,
                    CpuVersion = cpuVer,
                    ProgramCapacity = definedProgCapacity,
                    SelfDiagnosticError = match.Groups["sdiag"].Value,
                    OperationMode = (OPMode)byte.Parse(match.Groups["op"].Value, NumberStyles.HexNumber),
                };

                return true;

            }

            inf = default;
            return false;

        }

        /// <summary>
        /// Plc info when its not connected 
        /// </summary>
        public static PLCInfo None => new PLCInfo(null) {

            SelfDiagnosticError = "",
            CpuVersion = "",
            HardwareInformation = 0,
            OperationMode = 0,
            ProgramCapacity = 0,
            TypeCode = 0,

        };

        public override string ToString() {

            return $"{TypeName}, OP: {OperationMode}";

        }

        private protected void OnPropChange([CallerMemberName] string propertyName = null) {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

    }

}