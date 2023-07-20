using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    public interface IStringRegister : IRegister {

        /// <summary>
        /// The current value of the register
        /// </summary>
        string Value { get; }

        /// <summary>
        /// Reads the register value async from the plc
        /// </summary>
        /// <returns>The register value</returns>
        Task<string> ReadAsync();

        /// <summary>
        /// Writes the register content async to the plc
        /// </summary>
        /// <returns>True if successfully set</returns>
        Task WriteAsync(string data);

    }

}
