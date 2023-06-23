namespace MewtocolNet {

    /// <summary>
    /// The register prefixed type
    /// </summary>
    public enum RegisterType {

        /// <summary>
        /// Physical input as a bool (Relay)
        /// </summary>
        X = 0,
        /// <summary>
        /// Physical output as a bool (Relay)
        /// </summary>
        Y = 1,
        /// <summary>
        /// Internal as a bool (Relay)
        /// </summary>
        R = 2,
        /// <summary>
        /// Data area as a short (Register)
        /// </summary>
        DT_short = 3,
        /// <summary>
        /// Data area as an unsigned short (Register)
        /// </summary>
        DT_ushort = 4,
        /// <summary>
        /// Double data area as an integer  (Register)
        /// </summary>
        DDT_int = 5,
        /// <summary>
        /// Double data area as an unsigned integer (Register)
        /// </summary>
        DDT_uint = 6,
        /// <summary>
        /// Double data area as an floating point number (Register)
        /// </summary>
        DDT_float = 7,

    }

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
