namespace MewtocolNet.Registers {
    /// <summary>
    /// Contains generic information about the plc
    /// </summary>
    public class PLCInfo {

        /// <summary>
        /// Contains information about the PLCs cpu
        /// </summary>
        public CpuInfo CpuInformation {get;set;}
        /// <summary>
        /// Contains information about the PLCs operation modes
        /// </summary>
        public PLCMode OperationMode {get;set;}
        /// <summary>
        /// Current error code of the PLC
        /// </summary>
        public string ErrorCode {get;set;}

        /// <summary>
        /// Current station number of the PLC
        /// </summary>
        public int StationNumber { get;set;}        

        /// <summary>
        /// Generates a string containing some of the most important informations
        /// </summary>
        /// <returns></returns>
        public override string ToString () {

            return $"Type: {CpuInformation.Cputype},\n" +
                   $"Capacity: {CpuInformation.ProgramCapacity}k\n" +
                   $"CPU v: {CpuInformation.CpuVersion}\n" +
                   $"Station Num: {StationNumber}\n" +
                   $"--------------------------------\n" +
                   $"OP Mode: {(OperationMode.RunMode ? "Run" : "Prog")}\n" +
                   $"Error Code: {ErrorCode}";

        }

    }

}