﻿namespace MewtocolNet.Registers {

    /// <summary>
    /// Result for a read/write operation
    /// </summary>
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class NumberRegisterResult<T> {

        /// <summary>
        /// Command result
        /// </summary>
        public CommandResult Result { get; set; }

        /// <summary>
        /// The used register
        /// </summary>
        public NumberRegister<T> Register { get; set; }

        /// <summary>
        /// Trys to get the value of there is one
        /// </summary>
        public bool TryGetValue(out T value) {

            if (Result.Success) {
                value = (T)Register.Value;
                return true;
            }
            value = default;
            return false;

        }

    }

}
