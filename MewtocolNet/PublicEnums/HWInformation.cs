using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.PublicEnums {

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

    /// <summary>
    /// Descibes the operation mode of the device as flags
    /// </summary>
    [Flags]
    public enum OPMode : byte {

        /// <summary>
        /// No operation mode flag active
        /// </summary>
        None = 0,
        /// <summary>
        /// Is in RUN mode, otherwise its PROG Mode
        /// </summary>
        RunMode = 1,
        /// <summary>
        /// Is in test mode, otherwise ok
        /// </summary>
        TestMode = 2,
        /// <summary>
        /// Is BRK/1 step executed
        /// </summary>
        BreakPointPerOneStep = 4,
        /// <summary>
        /// Is BRK command enabled
        /// </summary>
        BreakEnabled = 16,
        /// <summary>
        /// Is outputting to external device
        /// </summary>
        ExternalOutput = 32,
        /// <summary>
        /// Is 1 step exec enabled
        /// </summary>
        OneStepExecEnabled = 64,
        /// <summary>
        /// Is a message displayed?
        /// </summary>
        MessageInstructionDisplayed = 128,
        /// <summary>
        /// Is in remote mode
        /// </summary>
        RemoteMode = 255,

    }

}
