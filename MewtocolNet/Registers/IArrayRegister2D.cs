using System.Collections.Generic;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Provides an abstruct enumerable interface for two dimensional array registers
    /// </summary>
    public interface IArrayRegister2D<T> : IReadOnlyList<T>, IRegister {

        /// <summary>
        /// Reads the register value async from the plc
        /// </summary>
        /// <returns>The register value</returns>
        Task<T[,]> ReadAsync();

        /// <summary>
        /// Writes a whole array to the plc
        /// </summary>
        /// <returns>True if successfully set</returns>
        Task WriteAsync(T[,] data);

        T[,] Value { get; }

        /// <summary>
        /// The current value of the register at the position
        /// </summary>
        T this[int i1, int i2] { get; }

    }

}
