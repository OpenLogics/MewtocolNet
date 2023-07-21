﻿using System;
using System.Threading.Tasks;
using MewtocolNet.Events;

namespace MewtocolNet.Registers {

    /// <summary>
    /// An interface for all register types
    /// </summary>
    public interface IRegister {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        event RegisterChangedEventHandler ValueChanged;

        /// <summary>
        /// Type of the underlying register
        /// </summary>
        RegisterPrefix RegisterType { get; }

        /// <summary>
        /// The name of the register
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the register address name as in the plc
        /// </summary>
        string PLCAddressName { get; }

        /// <summary>
        /// The current value of the register
        /// </summary>
        object ValueObj { get; }

        /// <summary>
        /// The plc memory address of the register
        /// </summary>
        uint MemoryAddress { get; }

        /// <summary>
        /// Gets the value of the register as the plc representation string
        /// </summary>
        /// <returns></returns>
        string GetAsPLC();

        /// <summary>
        /// Builds a readable string with all important register informations
        /// </summary>
        string ToString();

        /// <summary>
        /// Builds a readable string with all important register informations and additional infos
        /// </summary>
        string ToString(bool detailed);

    }

}
