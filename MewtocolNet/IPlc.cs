using MewtocolNet.Events;
using MewtocolNet.ProgramParsing;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.RegisterBuilding.BuilderPatterns;
using MewtocolNet.Registers;
using MewtocolNet.UnderlyingRegisters;
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
        /// Fires when the interface is fully connected to a PLC
        /// </summary>
        event PlcConnectionEventHandler Connected;

        /// <summary>
        /// Fires when a reconnect attempt was successfull
        /// </summary>
        event PlcConnectionEventHandler Reconnected;

        /// <summary>
        /// Fires when the interfaces makes a reconnect try to the PLC
        /// </summary>
        event PlcReconnectEventHandler ReconnectTryStarted;

        /// <summary>
        /// Fires when the plc/interface connection was fully closed
        /// </summary>
        event PlcConnectionEventHandler Disconnected;

        /// <summary>
        /// Fires when the value of a register changes
        /// </summary>
        event RegisterChangedEventHandler RegisterChanged;

        /// <summary>
        /// Plc mode was changed
        /// </summary>
        event PlcModeChangedEventHandler ModeChanged;

        /// <summary>
        /// The current connection state of the interface
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// This device is sending a message to the plc
        /// </summary>
        bool IsSending { get; } 

        /// <summary>
        /// The current transmission speed in bytes per second
        /// </summary>
        int BytesPerSecondUpstream { get; }

        /// <summary>
        /// This device is receiving a message from the plc
        /// </summary>
        bool IsReceiving { get; }

        /// <summary>
        /// The current transmission speed in bytes per second
        /// </summary>
        int BytesPerSecondDownstream { get; }

        /// <summary>
        /// Current poller cycle duration
        /// </summary>
        int PollerCycleDurationMs { get; }

        /// <summary>
        /// Shorthand indicator if the plc is in RUN mode
        /// </summary>
        bool IsRunMode { get; }

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

        IEnumerable<IRegister> Registers { get; }

        RBuildAnon Register { get; }

        /// <summary>
        /// Tries to establish a connection with the device asynchronously
        /// </summary>
        /// <param name="onConnected">A callback for excecuting something inside the plc connetion process</param>
        /// <returns></returns>
        Task<ConnectResult> ConnectAsync(Func<Task> onConnected = null);

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
        /// Sends a command to the PLC then awaits results<br/>
        /// The checksum and BCC are appended automatically
        /// </summary>
        /// <param name="_msg">MEWTOCOL Formatted request string ex: %01#RT</param>
        /// <returns>Returns the result</returns>
        //Task<MewtocolFrameResponse> SendCommandAsync(string _msg, Action<double> onReceiveProgress = null);

        /// <summary>
        /// Changes the PLCs operation mode to the given one
        /// </summary>
        /// <param name="setRun">True for RUN mode, false for PROG mode</param>
        /// <returns>The success state of the write operation</returns>
        Task<bool> SetOperationModeAsync(bool setRun);

        /// <summary>
        /// Toggles between RUN and PROG mode
        /// </summary>
        /// <returns>The success state of the write operation</returns>
        Task<bool> ToggleOperationModeAsync();

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

        /// <summary>
        /// A readonly list of the underlying memory areas
        /// </summary>
        IReadOnlyList<IMemoryArea> MemoryAreas { get; }

    }

}
