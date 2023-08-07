using System;

namespace MewtocolNet {
    /// <summary>
    /// Contains useful information about the PLC program and metadata
    /// </summary>
    public class PlcMetadata {

        /// <summary>
        /// The last date the used user librarys were changed
        /// </summary>
        public DateTime LastUserLibChangeDate { get; internal set; }

        /// <summary>
        /// The last date the program Pou's were changed
        /// </summary>
        public DateTime LastPouChangeDate { get; internal set; }

        /// <summary>
        /// The last date the PLC configuration was changed
        /// </summary>
        public DateTime LastConfigChangeDate { get; internal set; }

        /// <summary>
        /// The used FP-Win version to create the PLC program
        /// </summary>
        public string FPWinVersion { get; internal set; }

        /// <summary>
        /// The custom project version of the PLC program
        /// </summary>
        public string ProjectVersion { get; internal set; }

        /// <summary>
        /// Metadata format version
        /// </summary>
        public string MetaDataVersion { get; internal set; }

        /// <summary>
        /// The project ID of the PLC program
        /// </summary>
        public uint ProjectID { get; internal set; }

        /// <summary>
        /// The application / machine specific ID for the PLC program
        /// </summary>
        public uint ApplicationID { get; internal set; }

        /// <summary>
        /// The company ID of the PLC program creator
        /// </summary>
        public uint CompanyID { get; internal set; }

    }

}