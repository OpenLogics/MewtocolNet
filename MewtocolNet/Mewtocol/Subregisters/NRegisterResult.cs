using System;

namespace MewtocolNet.Registers {
    /// <summary>
    /// Result for a read/write operation
    /// </summary>
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class NRegisterResult<T> {

        /// <summary>
        /// Command result
        /// </summary>
        public CommandResult Result { get; set; }

        /// <summary>
        /// The used register
        /// </summary>
        public NRegister<T> Register { get; set; }

        /// <summary>
        /// Trys to get the value of there is one
        /// </summary>
        public bool TryGetValue (out T value) {

            if(Result.Success) {
                value = (T)Register.Value;
                return true;
            }
            value = default(T);
            return false;
        
        }

    }




}
