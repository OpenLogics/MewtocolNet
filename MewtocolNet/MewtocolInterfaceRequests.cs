using MewtocolNet.Logging;
using MewtocolNet.ProgramParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MewtocolNet.Helpers;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;

namespace MewtocolNet {

    public abstract partial class MewtocolInterface {

        internal bool isConnectingStage = false;

        internal protected bool isReconnectingStage = false;

        internal int maxDataBlocksPerWrite = 8;

        private CancellationTokenSource tTaskCancelSource = new CancellationTokenSource();

        #region PLC info getters

        /// <summary>
        /// Gets generic information about the PLC
        /// </summary>
        /// <returns>A PLCInfo class</returns>
        public async Task<PLCInfo> GetInfoAsync(bool detailed = true) {

            MewtocolFrameResponse resRT = await SendCommandInternalAsync("%EE#RT");

            if (!resRT.Success || tSource.Token.IsCancellationRequested) return null;

            MewtocolFrameResponse? resEXRT = null;

            if (isConnectingStage && detailed) {

                resEXRT = await SendCommandInternalAsync("%EE#EX00RT00");

            }

            //timeouts are ok and dont throw
            if (!resRT.Success && resRT == MewtocolFrameResponse.Timeout) return null;

            PLCInfo plcInf;

            //dont overwrite, use first
            if (!PLCInfo.TryFromRT(resRT.Response, out plcInf)) {

                throw new Exception("The RT message could not be parsed");

            }

            if(!detailed) return plcInf;

            //overwrite first with EXRT only on connecting stage
            if (isConnectingStage && resEXRT != null && resEXRT.Value.Success && !plcInf.TryExtendFromEXRT(resEXRT.Value.Response)) {

                throw new Exception("The EXRT message could not be parsed");

            }

            if (isConnectingStage) {
                //set the intial obj
                PlcInfo = plcInf;
            } else {
                //update the obj with RT dynamic values only
                PlcInfo.SelfDiagnosticError = plcInf.SelfDiagnosticError;
                PlcInfo.OperationMode = plcInf.OperationMode;
            }

            return PlcInfo;

        }

        /// <summary>
        /// Gets the metadata information for the PLC program
        /// </summary>
        /// <returns>The metadata or null of it isn't used by the PLC program</returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task<PlcMetadata> GetMetadataAsync() {

            //if the prog capacity and plc type are unknown retrieve them first
            if (PlcInfo == null) await GetInfoAsync();

            //still 0 
            if (PlcInfo.ProgramCapacity == 0) throw new NotSupportedException("Unable to access the program capacity of the PLC");

            //meta data is always at last dt addresses of the plc
            //so we take the capacity in k and multiply by 1024 to get the last register index and sub 3
            //to get the last readable registers

            var endAddress = (int)PlcInfo.ProgramCapacity * 1024 - 3; //32765 for 32k
            var readBytes = 42;

            var metaMarker = new byte[] { 0x4D, 0x65, 0x74, 0x41 };

            var data = await ReadAreaByteRangeAsync(endAddress - 2 - (readBytes / 2), readBytes);

            if (data != null && data.SearchBytePattern(metaMarker) == readBytes - 4) {

                var meta = new PlcMetadata {

                    LastUserLibChangeDate = PlcBitConverter.ToDateTime(data, 0),
                    LastPouChangeDate = PlcBitConverter.ToDateTime(data, 4),
                    LastConfigChangeDate = PlcBitConverter.ToDateTime(data, 8),
                    FPWinVersion = PlcBitConverter.ToVersionNumber(data, 12),
                    ProjectVersion = PlcBitConverter.ToVersionNumber(data, 16),
                    ProjectID = BitConverter.ToUInt32(data, 20),
                    ApplicationID = BitConverter.ToUInt32(data, 24),
                    CompanyID = BitConverter.ToUInt32(data, 28),
                    MetaDataVersion = PlcBitConverter.ToVersionNumber(data, 32),
                };

                PlcInfo.Metadata = meta;

                return meta;

            }

            return null;

        }

        #endregion

        #region Operation mode changing 

        /// <inheritdoc/>
        public async Task<bool> SetOperationModeAsync(bool setRun) {

            string modeChar = setRun ? "R" : "P";

            string requeststring = $"%{GetStationNumber()}#RM{modeChar}";
            var result = await SendCommandInternalAsync(requeststring);

            if (result.Success) {
            
                Logger.Log($"Operation mode was changed to {(setRun ? "Run" : "Prog")}", LogLevel.Info, this);

                //directily update the op mode
                PlcInfo.OperationMode = PlcInfo.OperationMode.SetFlag(OPMode.RunMode, setRun);

            } else {
                Logger.Log("Operation mode change failed", LogLevel.Error, this);
            }

            return result.Success;

        }

        /// <inheritdoc/>
        public async Task<bool> RestartProgramAsync() {

            return await SetOperationModeAsync(false) &&
                   await SetOperationModeAsync(true);

        }

        /// <inheritdoc/>
        public async Task<bool> ToggleOperationModeAsync() {

            var currMode = await GetInfoAsync(false);

            return await SetOperationModeAsync(!currMode.IsRunMode);

        }

        /// <inheritdoc/>
        public async Task FactoryResetAsync() {

            //set to prog mode
            await SetOperationModeAsync(false);

            //reset plc
            await SendCommandInternalAsync($"%{GetStationNumber()}#0F");
            await SendCommandInternalAsync($"%{GetStationNumber()}#21");

        }

        #endregion

        #region Program Read / Write

        public async Task<PlcBinaryProgram> ReadProgramAsync() {

            var steps = new List<byte[]>();

            int i = 0;
            int stepsPerReq = 50;
            while (i < int.MaxValue) {

                var sb = new StringBuilder($"%{GetStationNumber()}#RP");
                var stp1 = (i * stepsPerReq);
                var stp2 = ((i + 1) * stepsPerReq) - 1;

                sb.Append(stp1.ToString().PadLeft(5, '0'));
                sb.Append(stp2.ToString().PadLeft(5, '0'));

                var res = await SendCommandInternalAsync(sb.ToString());

                if (res.Success) {

                    var bytes = res.Response.ParseResponseStringAsBytes();
                    var foundEndPattern = bytes.SearchBytePattern(new byte[] { 0xF8, 0xFF, 0xFF });

                    for (int j = 0; j < bytes.Length; j += 2) {
                        var split = bytes.Skip(j).Take(2).ToArray();
                        if (split[0] == 0xFF && split[1] == 0xFF) break;
                        steps.Add(split);
                    }

                    if (foundEndPattern != -1) {

                        break;

                    }

                }

                i++;

            }

            return new PlcBinaryProgram {
                rawSteps = steps,
            };

        }

        #endregion

        #region Byte range writing / reading to registers

        /// <summary>
        /// Writes a byte array to a span over multiple registers at once,
        /// Rembember the plc can only store word so in order to write to a word array 
        /// your byte array should be double the size
        /// </summary>
        /// /// <param name="start">start address of the array</param>
        /// <param name="byteArr"></param>
        /// <returns></returns>
        public async Task<bool> WriteAreaByteRange(int start, byte[] byteArr) {

            if (byteArr == null)
                throw new ArgumentNullException(nameof(byteArr));

            string byteString = byteArr.ToHexString();

            var wordLength = byteArr.Length / 2;
            if (byteArr.Length % 2 != 0) wordLength++;

            string startStr = start.ToString().PadLeft(5, '0');
            string endStr = (start + wordLength - 1).ToString().PadLeft(5, '0');

            string requeststring = $"%{GetStationNumber()}#WDD{startStr}{endStr}{byteString}";
            var result = await SendCommandInternalAsync(requeststring);

            return result.Success;

        }

        /// <summary>
        /// Reads the bytes from the start adress for counts byte length, 
        /// doesn't block the receive thread
        /// </summary>
        /// <param name="start">Start adress</param>
        /// <param name="byteCount">Number of bytes to get</param>
        /// <param name="onProgress">Gets invoked when the progress changes, contains the progress as a double from 0 - 1.0</param>
        /// <returns>A byte array of the requested DT area</returns>
        public async Task<byte[]> ReadAreaByteRangeAsync(int start, int byteCount, RegisterPrefix areaPrefix = RegisterPrefix.DT, Action<double> onProgress = null) {

            //on odd bytes add one word
            var wordLength = byteCount / 2;
            if (byteCount % 2 != 0) wordLength++;

            int maxReadBlockSize = maxDataBlocksPerWrite;

            if (byteCount < (maxReadBlockSize * 2)) maxReadBlockSize = wordLength;

            int blocksToReadNoOverflow = wordLength / maxReadBlockSize;
            int blocksOverflow = wordLength % maxReadBlockSize;
            int totalBlocksToRead = blocksOverflow != 0 ? blocksToReadNoOverflow + 1 : blocksToReadNoOverflow;

            List<byte> readBytes = new List<byte>();

            int padLeftLen = 0;
            string areaCodeStr = null;

            switch (areaPrefix) {
                case RegisterPrefix.X:
                areaCodeStr = $"RCCX";
                padLeftLen = 4;
                break;
                case RegisterPrefix.Y:
                areaCodeStr = $"RCCY";
                padLeftLen = 4;
                break;
                case RegisterPrefix.R:
                areaCodeStr = $"RCCR";
                padLeftLen = 4;
                break;
                case RegisterPrefix.DT:
                case RegisterPrefix.DDT:
                areaCodeStr = $"RDD";
                padLeftLen = 5;
                break;
            }

            async Task ReadBlock(int wordStart, int wordEnd, Action<double> readProg) {

                int blockSize = wordEnd - wordStart + 1;
                string startStr = wordStart.ToString().PadLeft(padLeftLen, '0');
                string endStr = wordEnd.ToString().PadLeft(padLeftLen, '0');
                string requeststring = $"%{GetStationNumber()}#{areaCodeStr}{startStr}{endStr}";

                var result = await SendCommandInternalAsync(requeststring, onReceiveProgress: readProg);

                if (result.Success && !string.IsNullOrEmpty(result.Response)) {

                    var bytes = result.Response.ParseResponseStringAsBytes();

                    if (bytes != null)
                        readBytes.AddRange(bytes);

                }

            }

            //get all full blocks
            for (int i = 0; i < blocksToReadNoOverflow; i++) {

                int curWordStart, curWordEnd;

                curWordStart = start + (i * maxReadBlockSize);
                curWordEnd = curWordStart + maxReadBlockSize - 1;

                await ReadBlock(curWordStart, curWordEnd, (p) => {

                    if (onProgress != null && p != 0) {
                        var toplevelProg = (double)(i + 1) / totalBlocksToRead;
                        onProgress(toplevelProg * p);
                    }

                });

                //read remaining block
                if (i == blocksToReadNoOverflow - 1 && blocksOverflow != 0) {

                    if (onProgress != null)
                        onProgress((double)readBytes.Count / byteCount);

                    curWordStart = start + ((i + 1) * maxReadBlockSize);
                    curWordEnd = curWordStart + blocksOverflow - 1;

                    await ReadBlock(curWordStart, curWordEnd, (p) => { });

                }

            }

            if (onProgress != null)
                onProgress((double)1);

            return readBytes.ToArray();

        }

        #endregion

        #region Helpers

        internal string GetStationNumber() {

            if (StationNumber != 0xEE && StationNumber > 99)
                throw new NotSupportedException("Station number was greater 99");

            if (StationNumber == 0xEE) return "EE";

            return StationNumber.ToString().PadLeft(2, '0');

        }

        #endregion

    }

}
