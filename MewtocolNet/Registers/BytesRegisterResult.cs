namespace MewtocolNet.Registers {

    /// <summary>
    /// The results of a string register operation
    /// </summary>
    public class BytesRegisterResult<T> {

        /// <summary>
        /// The command result
        /// </summary>
        public CommandResult Result { get; set; }
        /// <summary>
        /// The register definition used
        /// </summary>
        public BytesRegister<T> Register { get; set; }

    }

}
