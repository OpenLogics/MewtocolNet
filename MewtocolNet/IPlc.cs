using MewtocolNet.ProgramParsing;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace MewtocolNet {

    /// <summary>
    /// Provides a interface for Panasonic PLCs
    /// </summary>
    public interface IPlc : IDisposable, INotifyPropertyChanged {

        /// <summary>
        /// The current connection state of the interface
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The current transmission speed in bytes per second
        /// </summary>
        int BytesPerSecondUpstream { get; }

        /// <summary>
        /// The current transmission speed in bytes per second
        /// </summary>
        int BytesPerSecondDownstream { get; }

        /// <summary>
        /// Current poller cycle duration
        /// </summary>
        int PollerCycleDurationMs { get; }

        /// <summary>
        /// Currently queued message count
        /// </summary>
        int QueuedMessages { get; }

        /// <summary>
        /// Generic information about the connected PLC
        /// </summary>
        PLCInfo PlcInfo { get; }

        /// <summary>
        /// The station number of the PLC
        /// </summary>
        int StationNumber { get; }

        /// <summary>
        /// A connection info string
        /// </summary>
        string ConnectionInfo { get; }

        /// <summary>
        /// The initial connection timeout in milliseconds
        /// </summary>
        int ConnectTimeout { get; set; }

        /// <summary>
        /// Tries to establish a connection with the device asynchronously
        /// </summary>
        /// <param name="onConnected">A callback for excecuting something right after the plc connected</param>
        /// <returns></returns>
        Task ConnectAsync(Func<Task> onConnected = null);

        /// <summary>
        /// Disconnects the device from its current plc connection 
        /// and awaits the end of all asociated tasks
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Disconnects the device from its current plc connection
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Calculates the checksum automatically and sends a command to the PLC then awaits results
        /// </summary>
        /// <param name="_msg">MEWTOCOL Formatted request string ex: %01#RT</param>
        /// <param name="withTerminator">Append the checksum and bcc automatically</param>
        /// <param name="timeoutMs">Timout to wait for a response</param>
        /// <returns>Returns the result</returns>
        Task<MewtocolFrameResponse> SendCommandAsync(string _msg, bool withTerminator = true, int timeoutMs = -1, Action<double> onReceiveProgress = null);

        /// <summary>
        /// Changes the PLCs operation mode to the given one
        /// </summary>
        /// <param name="setRun">True for run mode, false for prog mode</param>
        /// <returns>The success state of the write operation</returns>
        Task<bool> SetOperationModeAsync(bool setRun);

        /// <summary>
        /// Restarts the plc program
        /// </summary>
        /// <returns>The success state of the write operation</returns>
        Task<bool> RestartProgramAsync();

        /// <summary>
        /// Reads the program from the connected plc
        /// </summary>
        /// <returns></returns>
        Task<PlcBinaryProgram> ReadProgramAsync();

        /// <summary>
        /// Factory resets the PLC, this includes the current program
        /// and data in the EEPROM
        /// </summary>
        /// <returns></returns>
        Task FactoryResetAsync();

        /// <summary>
        /// Use this to await the first poll iteration after connecting,
        /// This also completes if the initial connection fails
        /// </summary>
        Task AwaitFirstDataCycleAsync();

        /// <summary>
        /// Runs a single poller cycle manually,
        /// useful if you want to use a custom update frequency
        /// </summary>
        /// <returns>The number of inidvidual mewtocol commands sent</returns>
        Task<int> UpdateAsync();

        /// <summary>
        /// Gets the connection info string
        /// </summary>
        string GetConnectionInfo();

        /// <summary>
        /// Gets a register from the plc by name
        /// </summary>
        IRegister GetRegister(string name);

        /// <summary>
        /// Gets all registers from the plc
        /// </summary>
        IEnumerable<IRegister> GetAllRegisters();

        /// <summary>
        /// Explains the register internal layout at this moment in time
        /// </summary>
        string Explain();

    }

}
