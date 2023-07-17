using System;
using System.Threading.Tasks;
using MewtocolNet.Events;

namespace MewtocolNet.Registers {

    /// <summary>
    /// An interface for all register types
    /// </summary>
    public interface IRegister<T> : IRegister where T : struct {

        /// <summary>
        /// The current value of the register
        /// </summary>
        T? Value { get; }

        /// <summary>
        /// Reads the register value async from the plc
        /// </summary>
        /// <returns>The register value</returns>
        Task<T?> ReadAsync();

        /// <summary>
        /// Writes the register content async to the plc
        /// </summary>
        /// <returns>True if successfully set</returns>
        Task<bool> WriteAsync(T data);

    }

}
