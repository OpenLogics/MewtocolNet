﻿namespace MewtocolNet {

    /// <summary>
    /// The register prefixed type
    /// </summary>
    public enum RegisterPrefix {

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

    }

}
