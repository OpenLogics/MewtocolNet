using System;

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
        /// Single word area (Register)
        /// </summary>
        DT = 3,
        /// <summary>
        /// Double word area (Register)
        /// </summary>
        DDT = 4,
        /// <summary>
        /// Area of a byte sequence longer than 2 words 
        /// </summary>
        DT_RANGE = 5,

    }

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
