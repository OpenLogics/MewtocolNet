using MewtocolNet.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MewtocolNet {

    public abstract partial class MewtocolInterface {

        internal bool isConnectingStage = false;

        internal int maxDataBlocksPerWrite = 8;

        #region PLC info getters

        /// <summary>
        /// Gets generic information about the PLC
        /// </summary>
        /// <returns>A PLCInfo class</returns>
        public async Task<PLCInfo> GetPLCInfoAsync(int timeout = -1) {

            MewtocolFrameResponse resRT = await SendCommandAsync("%EE#RT", timeoutMs: timeout);

            if (!resRT.Success) {

                //timeouts are ok and don't throw
                if (resRT == MewtocolFrameResponse.Timeout) return null;

                throw new Exception(resRT.Error);

            }

            MewtocolFrameResponse? resEXRT = null;

            if(isConnectingStage) {

                resEXRT = await SendCommandAsync("%EE#EX00RT00", timeoutMs: timeout);

            }

            //timeouts are ok and dont throw
            if (!resRT.Success && resRT == MewtocolFrameResponse.Timeout) return null;

            PLCInfo plcInf;

            //dont overwrite, use first
            if (!PLCInfo.TryFromRT(resRT.Response, out plcInf)) {

                throw new Exception("The RT message could not be parsed");

            }

            //overwrite first with EXRT only on connecting stage
            if (isConnectingStage && resEXRT != null && resEXRT.Value.Success && !plcInf.TryExtendFromEXRT(resEXRT.Value.Response)) {

                throw new Exception("The EXRT message could not be parsed");

            } 

            if(isConnectingStage) {
                //set the intial obj
                PlcInfo = plcInf;
            } else {
                //update the obj with RT dynamic values only
                PlcInfo.SelfDiagnosticError = plcInf.SelfDiagnosticError;   
                PlcInfo.OperationMode = plcInf.OperationMode;
            }

            return PlcInfo;

        }

        #endregion

        #region Operation mode changing 

        /// <inheritdoc/>
        public async Task<bool> SetOperationModeAsync(bool setRun) {

            string modeChar = setRun ? "R" : "P";

            string requeststring = $"%{GetStationNumber()}#RM{modeChar}";
            var result = await SendCommandAsync(requeststring);

            if (result.Success) {
                Logger.Log($"Operation mode was changed to {(setRun ? "Run" : "Prog")}", LogLevel.Info, this);
            } else {
                Logger.Log("Operation mode change failed", LogLevel.Error, this);
            }

            return result.Success;

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
        public async Task<bool> WriteByteRange(int start, byte[] byteArr) {

            if (byteArr == null)
                throw new ArgumentNullException(nameof(byteArr));

            string byteString = byteArr.ToHexString();

            var wordLength = byteArr.Length / 2;
            if (byteArr.Length % 2 != 0) wordLength++;

            string startStr = start.ToString().PadLeft(5, '0');
            string endStr = (start + wordLength - 1).ToString().PadLeft(5, '0');

            string requeststring = $"%{GetStationNumber()}#WDD{startStr}{endStr}{byteString}";
            var result = await SendCommandAsync(requeststring);

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
        public async Task<byte[]> ReadByteRangeNonBlocking(int start, int byteCount, Action<double> onProgress = null) {

            //on odd bytes add one word
            var wordLength = byteCount / 2;
            if (byteCount % 2 != 0) wordLength++;

            int maxReadBlockSize = maxDataBlocksPerWrite;

            if (byteCount < (maxReadBlockSize * 2)) maxReadBlockSize = wordLength;

            int blocksToReadNoOverflow = wordLength / maxReadBlockSize;
            int blocksOverflow = wordLength % maxReadBlockSize;
            int totalBlocksToRead = blocksOverflow != 0 ? blocksToReadNoOverflow + 1 : blocksToReadNoOverflow;

            List<byte> readBytes = new List<byte>();    

            async Task ReadBlock (int wordStart, int wordEnd, Action<double> readProg) {

                int blockSize = wordEnd - wordStart + 1;
                string startStr = wordStart.ToString().PadLeft(5, '0');
                string endStr = wordEnd.ToString().PadLeft(5, '0');

                string requeststring = $"%{GetStationNumber()}#RDD{startStr}{endStr}";

                var result = await SendCommandAsync(requeststring, onReceiveProgress: readProg);

                if (result.Success && !string.IsNullOrEmpty(result.Response)) {

                    var bytes = result.Response.ParseDTRawStringAsBytes();
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

                    await ReadBlock(curWordStart, curWordEnd, (p) => {});

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
