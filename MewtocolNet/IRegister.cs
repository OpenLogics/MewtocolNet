using System;
using System.Threading.Tasks;

namespace MewtocolNet {

    /// <summary>
    /// An interface for all register types
    /// </summary>
    public interface IRegister {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        event Action<object> ValueChanged;

        /// <summary>
        /// Type of the underlying register
        /// </summary>
        RegisterType RegisterType { get; }  

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
        object Value { get; }

        /// <summary>
        /// The plc memory address of the register
        /// </summary>
        int MemoryAddress { get; }

        /// <summary>
        /// Builds a readable string with all important register informations
        /// </summary>
        string ToString();

        /// <summary>
        /// Builds a readable string with all important register informations and additional infos
        /// </summary>
        string ToString(bool detailed);

        /// <summary>
        /// Sets the register value in the plc async
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> SetValueAsync();

        /// <summary>
        /// Gets the register value from the plc async
        /// </summary>
        /// <returns>The value or null if failed</returns>
        Task<object> GetValueAsync();

    }

}
