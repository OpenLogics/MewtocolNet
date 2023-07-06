namespace MewtocolNet {

    // this is just used as syntactic sugar,
    // when creating registers that are R/X/Y typed you dont need the DT types

    /// <summary>
    /// The type of an input/output register
    /// </summary>
    public enum IOType {

        /// <summary>
        /// Physical input as a bool (Relay)
        /// </summary>
        X = 0,
        /// <summary>
        /// Physical output as a bool (Relay)
        /// </summary>
        Y = 1,
        /// <summary>
        /// Internal relay
        /// </summary>
        R = 2,

    }

}
