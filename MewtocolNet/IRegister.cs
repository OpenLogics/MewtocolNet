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
        /// The current value of the register
        /// </summary>
        object Value { get; }

        /// <summary>
        /// The plc memory address of the register
        /// </summary>
        int MemoryAddress { get; }

        /// <summary>
        /// Gets the special address of the register or -1 if it has none
        /// </summary>
        /// <returns></returns>
        byte? GetSpecialAddress();

        /// <summary>
        /// Indicates if the register is processed bitwise
        /// </summary>
        /// <returns>True if bitwise</returns>
        bool IsUsedBitwise();

        /// <summary>
        /// Generates a string describing the starting memory address of the register
        /// </summary>
        string GetStartingMemoryArea();

        /// <summary>
        /// Gets the current value formatted as a readable string
        /// </summary>
        string GetValueString();

        /// <summary>
        /// Builds the identifier for the mewtocol query string
        /// </summary>
        /// <returns></returns>
        string BuildMewtocolQuery();

        /// <summary>
        /// Builds a register string that prepends the memory address fe. DDT or DT, X, Y etc
        /// </summary>
        string GetRegisterString();

        /// <summary>
        /// Builds a combined name for the attached property to uniquely identify the property register binding
        /// </summary>
        /// <returns></returns>
        string GetCombinedName();

        /// <summary>
        /// Gets the name of the class that contains the attached property
        /// </summary>
        /// <returns></returns>
        string GetContainerName();

        /// <summary>
        /// Builds a register name after the PLC convention <br/>
        /// Example <code>DDT100, XA, X6, Y1, DT3300</code>
        /// </summary>
        string GetRegisterPLCName();

        /// <summary>
        /// Clears the current value of the register and resets it to default
        /// </summary>
        void ClearValue();

        /// <summary>
        /// Triggers a notifychanged update event
        /// </summary>
        void TriggerNotifyChange();

        /// <summary>
        /// Gets the type of the class collection its attached property is in or null
        /// </summary>
        /// <returns>The class name or null if manually added</returns>
        Type GetCollectionType();

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
