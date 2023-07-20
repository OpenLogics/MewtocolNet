using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Provides an abstruct enumerable interface for one dimensional array registers
    /// </summary>
    public interface IArrayRegister<T> : IReadOnlyList<T>, IRegister {

        /// <summary>
        /// Reads the register value async from the plc
        /// </summary>
        /// <returns>The register value</returns>
        Task<T[]> ReadAsync();

        /// <summary>
        /// Writes a whole array to the plc
        /// </summary>
        /// <returns>True if successfully set</returns>
        Task WriteAsync(T[] data);

        ///// <summary>
        ///// Writes a single item to the array, this saves bandwidth
        ///// </summary>
        ///// <param name="i">Index of the element to write</param>
        ///// <param name="data">The value to overwrite</param>
        ///// <returns>True if successfully set</returns>
        //Task WriteEntryAsync(int i, T data);

        T[] Value { get; }  

        /// <summary>
        /// The current value of the register at the position
        /// </summary>
        new T this[int i] { get; }

    }

}
