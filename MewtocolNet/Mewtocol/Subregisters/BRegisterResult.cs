namespace MewtocolNet.Registers {

    /// <summary>
    /// Result for a boolean register
    /// </summary>
    public class BRegisterResult {

        /// <summary>
        /// The command result
        /// </summary>
        public CommandResult Result { get; set; }

        /// <summary>
        /// The used register
        /// </summary>
        public BRegister Register { get; set; }

    }

}
