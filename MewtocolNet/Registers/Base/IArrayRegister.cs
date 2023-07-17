using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    public interface IArrayRegister<T> : IRegister {

        /// <summary>
        /// The current value of the register
        /// </summary>
        T[] Value { get; }

        /// <summary>
        /// Reads the register value async from the plc
        /// </summary>
        /// <returns>The register value</returns>
        Task<T[]> ReadAsync();

        /// <summary>
        /// Writes the register content async to the plc
        /// </summary>
        /// <returns>True if successfully set</returns>
        Task<bool> WriteAsync(T data);

    }

}
