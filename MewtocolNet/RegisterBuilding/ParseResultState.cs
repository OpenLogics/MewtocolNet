namespace MewtocolNet.RegisterBuilding {
    internal enum ParseResultState {

        /// <summary>
        /// The parse try failed at the intial regex match
        /// </summary>
        FailedSoft,
        /// <summary>
        /// The parse try failed at the afer- regex match
        /// </summary>
        FailedHard,
        /// <summary>
        /// The parse try did work
        /// </summary>
        Success,

    }

}
