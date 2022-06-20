using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet { 

    /// <summary>
    /// The special register type
    /// </summary>
    public enum RegisterType {

        /// <summary>
        /// Physical input as a bool (Relay)
        /// </summary>
        X,
        /// <summary>
        /// Physical output as a bool (Relay)
        /// </summary>
        Y,
        /// <summary>
        /// Internal as a bool (Relay)
        /// </summary>
        R,
        /// <summary>
        /// Data area as a short (Register)
        /// </summary>
        DT_short,
        /// <summary>
        /// Data area as an unsigned short (Register)
        /// </summary>
        DT_ushort,
        /// <summary>
        /// Double data area as an integer  (Register)
        /// </summary>
        DDT_int,
        /// <summary>
        /// Double data area as an unsigned integer (Register)
        /// </summary>
        DDT_uint,
        /// <summary>
        /// Double data area as an floating point number (Register)
        /// </summary>
        DDT_float,

    }

    /// <summary>
    /// The special input / output channel address
    /// </summary>
    public enum SpecialAddress {

        #pragma warning disable CS1591

        /// <summary>
        /// No defined
        /// </summary>
        None,
        A = -10,
        B = -11,
        C = -12,
        D = -13,
        E = -14,
        F = -15,

        #pragma warning restore

    }

}
