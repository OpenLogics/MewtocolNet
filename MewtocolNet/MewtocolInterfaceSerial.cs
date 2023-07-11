﻿using MewtocolNet.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MewtocolNet.RegisterAttributes;

namespace MewtocolNet {
    
    public sealed class MewtocolInterfaceSerial : MewtocolInterface, IPlcSerial {

        private bool autoSerial;

        private event Action tryingSerialConfig;

        //serial config

        /// <inheritdoc/>
        public string PortName { get; private set; }

        /// <inheritdoc/>
        public int SerialBaudRate { get; private set; }

        /// <inheritdoc/>
        public int SerialDataBits { get; private set; }

        /// <inheritdoc/>
        public Parity SerialParity { get; private set; }

        /// <inheritdoc/>
        public StopBits SerialStopBits { get; private set; }

        //Serial
        internal SerialPort serialClient;

        internal MewtocolInterfaceSerial () : base() { }

        /// <inheritdoc/>
        public IPlcSerial WithPoller () {
             
            usePoller = true;
            return this;

        }

        /// <inheritdoc/>
        public override string GetConnectionInfo() {

            StringBuilder sb = new StringBuilder();

            sb.Append($"{PortName}, ");
            sb.Append($"{SerialBaudRate}, ");
            sb.Append($"{SerialDataBits} ");

            sb.Append($"{SerialParity.ToString().Substring(0, 1)} ");

            switch (SerialStopBits) {
                case StopBits.None:
                sb.Append("0");
                break;
                case StopBits.One:
                sb.Append("1");
                break;
                case StopBits.Two:
                sb.Append("2");
                break;
                case StopBits.OnePointFive:
                sb.Append("1.5");
                break;
            }

            return sb.ToString();

        }

        /// <inheritdoc/>
        public void ConfigureConnection (string _portName, int _baudRate = 19200, int _dataBits = 8, Parity _parity = Parity.Odd, StopBits _stopBits = StopBits.One, int _station = 1) {

            PortName = _portName;
            SerialBaudRate = _baudRate;
            SerialDataBits = _dataBits;    
            SerialParity = _parity;    
            SerialStopBits = _stopBits;
            stationNumber = _station;

            OnSerialPropsChanged();
            Disconnect();

        }

        internal void ConfigureConnectionAuto () {

            autoSerial = true;

        }

        public override async Task ConnectAsync() => await ConnectAsync(null);

        /// <inheritdoc/>
        public async Task ConnectAsync (Action onTryingConfig = null) {

            void OnTryConfig() {
                onTryingConfig();
            }

            if (onTryingConfig != null)
                tryingSerialConfig += OnTryConfig;

            try {

                PLCInfo? gotInfo = null;

                if(autoSerial) {

                    Logger.Log($"Connecting [AUTO CONFIGURE]: {PortName}", LogLevel.Info, this);
                    gotInfo = await TryConnectAsyncMulti();

                } else {

                    Logger.Log($"Connecting [MAN]: {PortName}", LogLevel.Info, this);
                    gotInfo = await TryConnectAsyncSingle(PortName, SerialBaudRate, SerialDataBits, SerialParity, SerialStopBits);

                }

                if(gotInfo != null) {

                    OnConnected(gotInfo.Value);

                } else {

                    Logger.Log("Initial connection failed", LogLevel.Error, this);
                    OnMajorSocketExceptionWhileConnecting();

                }

                await Task.CompletedTask;

            } catch (SocketException) {

                OnMajorSocketExceptionWhileConnecting();

            }

            tryingSerialConfig -= OnTryConfig;

        }

        private async Task<PLCInfo?> TryConnectAsyncMulti () {

            var baudRates = Enum.GetValues(typeof(BaudRate)).Cast<BaudRate>();

            //ordered by most commonly used
            baudRates = new List<BaudRate> { 
                //most common 3
                BaudRate._19200,
                BaudRate._115200,
                BaudRate._9600,   
                //others
                BaudRate._1200,
                BaudRate._2400,
                BaudRate._4800,
                BaudRate._38400,
                BaudRate._57600,
                BaudRate._230400, 
            };

            var dataBits = Enum.GetValues(typeof(DataBits)).Cast<DataBits>();
            var parities = new List<Parity>() { Parity.None, Parity.Odd, Parity.Even, Parity.Mark };
            var stopBits = new List<StopBits> { StopBits.One, StopBits.Two };

            foreach (var baud in baudRates) {

                foreach (var databit in dataBits) {

                    foreach (var parity in parities) {

                        foreach (var stopBit in stopBits) {

                            var res = await TryConnectAsyncSingle(PortName, (int)baud, (int)databit, parity, stopBit);
                            if(res != null) return res;

                        }

                    }

                }

            }

            return null;

        }

        private async Task<PLCInfo?> TryConnectAsyncSingle (string port, int baud, int dbits, Parity par, StopBits sbits) {

            try {

                serialClient = new SerialPort() {
                    PortName = port,
                    BaudRate = baud,
                    DataBits = dbits,
                    Parity = par,
                    StopBits = sbits,
                    ReadTimeout = 100,
                    Handshake = Handshake.None
                };

                PortName = port;
                SerialBaudRate = baud;
                SerialDataBits = dbits;
                SerialParity = par;
                SerialStopBits = sbits;
                OnSerialPropsChanged();
                tryingSerialConfig?.Invoke();

                serialClient.Open();

                if (!serialClient.IsOpen) {

                    Logger.Log($"Failed to open [SERIAL]: {GetConnectionInfo()}", LogLevel.Critical, this);
                    return null;

                }

                stream = serialClient.BaseStream;

                Logger.Log($"Opened [SERIAL]: {GetConnectionInfo()}", LogLevel.Critical, this);

                var plcinf = await GetPLCInfoAsync(100);

                if (plcinf == null) CloseClient();

                return plcinf;

            } catch (UnauthorizedAccessException) {

                Logger.Log($"The port {serialClient.PortName} is currently in use. Close all accessing applications first", LogLevel.Error, this);
                return null;

            }

        }

        private void CloseClient () {

            if(serialClient.IsOpen) {

                serialClient.Close();
                Logger.Log($"Closed [SERIAL]", LogLevel.Verbose, this);

            }
            
        }

        private protected override void OnDisconnect() {

            if (IsConnected) {

                base.OnDisconnect();

                CloseClient();

            }

        }

        private void OnSerialPropsChanged () {

            OnPropChange(nameof(PortName));
            OnPropChange(nameof(SerialBaudRate));
            OnPropChange(nameof(SerialDataBits));
            OnPropChange(nameof(SerialParity));
            OnPropChange(nameof(SerialStopBits));

        }

    }

}
