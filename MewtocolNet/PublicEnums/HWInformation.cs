using System;

namespace MewtocolNet {

    /// <summary>
    /// Contains hardware information about the device as flags
    /// </summary>
    [Flags]
    public enum HWInformation : byte {

        /// <summary>
        /// Has user ROM
        /// </summary>
        UserROM = 1,
        /// <summary>
        /// Has IC card
        /// </summary>
        ICCard = 2,
        /// <summary>
        /// Has general purpose memory
        /// </summary>
        GeneralPurposeMemory = 4,
        /// <summary>
        /// Is CPU ultra high speed type
        /// </summary>
        UltraHighSpeed = 8,

    }

}
