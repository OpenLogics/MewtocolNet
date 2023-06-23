namespace MewtocolNet.Subregisters {

    /// <summary>
    /// The results of a string register operation
    /// </summary>
    public class SRegisterResult {

        /// <summary>
        /// The command result
        /// </summary>
        public CommandResult Result { get; set; }
        /// <summary>
        /// The register definition used
        /// </summary>
        public SRegister Register { get; set; }

    }

}
