﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MewtocolNet {

    /// <summary>
    /// Provides a interface for Panasonic PLCs
    /// </summary>
    public interface IPlc : IDisposable {

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
        /// The registered data registers of the PLC
        /// </summary>
        IEnumerable<IRegister> Registers { get; }

        /// <summary>
        /// Generic information about the connected PLC
        /// </summary>
        PLCInfo PlcInfo { get; }

        /// <summary>
        /// The station number of the PLC
        /// </summary>
        int StationNumber { get; }

        /// <summary>
        /// The initial connection timeout in milliseconds
        /// </summary>
        int ConnectTimeout { get; set; }

        /// <summary>
        /// Tries to establish a connection with the device asynchronously
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Disconnects the devive from its current connection
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Calculates the checksum automatically and sends a command to the PLC then awaits results
        /// </summary>
        /// <param name="_msg">MEWTOCOL Formatted request string ex: %01#RT</param>
        /// <param name="withTerminator">Append the checksum and bcc automatically</param>
        /// <param name="timeoutMs">Timout to wait for a response</param>
        /// <returns>Returns the result</returns>
        Task<MewtocolFrameResponse> SendCommandAsync(string _msg, bool withTerminator = true, int timeoutMs = -1);

        /// <summary>
        /// Use this to await the first poll iteration after connecting,
        /// This also completes if the initial connection fails
        /// </summary>
        Task AwaitFirstDataAsync();

        /// <summary>
        /// Runs a single poller cycle manually,
        /// useful if you want to use a custom update frequency
        /// </summary>
        /// <returns>The number of inidvidual mewtocol commands sent</returns>
        Task<int> RunPollerCylceManual();

        /// <summary>
        /// Gets the connection info string
        /// </summary>
        string GetConnectionInfo();

    }

}
