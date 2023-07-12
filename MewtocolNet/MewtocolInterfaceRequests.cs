using MewtocolNet.Exceptions;
using MewtocolNet.Logging;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet {

    public abstract partial class MewtocolInterface {

        #region PLC info getters

        /// <summary>
        /// Gets generic information about the PLC
        /// </summary>
        /// <returns>A PLCInfo class</returns>
        public async Task<PLCInfo?> GetPLCInfoAsync(int timeout = -1) {

            var resRT = await SendCommandAsync("%EE#RT", timeoutMs: timeout);
            
            if (!resRT.Success) {

                //timeouts are ok and dont throw
                if (resRT == MewtocolFrameResponse.Timeout) return null;

                throw new MewtocolException(resRT.Error);

            }

            var resEXRT = await SendCommandAsync("%EE#EX00RT00", timeoutMs: timeout);

            //timeouts are ok and dont throw
            if (!resRT.Success && resRT == MewtocolFrameResponse.Timeout) return null;

            PLCInfo plcInf;

            //dont overwrite, use first
            if (!PLCInfo.TryFromRT(resRT.Response, out plcInf)) {

                throw new MewtocolException("The RT message could not be parsed");

            }

            //overwrite first with EXRT
            if (resEXRT.Success && !plcInf.TryExtendFromEXRT(resEXRT.Response)) {

                throw new MewtocolException("The EXRT message could not be parsed");

            }

            PlcInfo = plcInf;   

            return plcInf;

        }

        #endregion

        #region Operation mode changing 

        /// <inheritdoc/>
        public async Task<bool> SetOperationModeAsync (bool setRun) {

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

        #region Byte range writingv / reading to registers

        /// <summary>
        /// Writes a byte array to a span over multiple registers at once,
        /// Rembember the plc can only store word so in order to write to a word array 
        /// your byte array should be double the size
        /// </summary>
        /// /// <param name="start">start address of the array</param>
        /// <param name="byteArr"></param>
        /// <returns></returns>
        public async Task<bool> WriteByteRange (int start, byte[] byteArr, bool flipBytes = false) {

            string byteString;

            if(flipBytes) {
                byteString = byteArr.BigToMixedEndian().ToHexString();
            } else {
                byteString = byteArr.ToHexString();
            }

            var wordLength = byteArr.Length / 2;
            if (byteArr.Length % 2 != 0)
                wordLength++;

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
        /// <param name="count">Number of bytes to get</param>
        /// <param name="flipBytes">Flips bytes from big to mixed endian</param>
        /// <param name="onProgress">Gets invoked when the progress changes, contains the progress as a double</param>
        /// <returns>A byte array or null of there was an error</returns>
        public async Task<byte[]> ReadByteRangeNonBlocking (int start, int count, bool flipBytes = false, Action<double> onProgress = null) {

            var byteList = new List<byte>();

            var wordLength = count / 2;
            if (count % 2 != 0)
                wordLength++;

            int blockSize = 8;

            //read blocks of max 4 words per msg
            for (int i = 0; i < wordLength; i += blockSize) {

                int curWordStart = start + i;
                int curWordEnd = curWordStart + blockSize - 1;

                string startStr = curWordStart.ToString().PadLeft(5, '0');
                string endStr = (curWordEnd).ToString().PadLeft(5, '0');

                string requeststring = $"%{GetStationNumber()}#RDD{startStr}{endStr}";
                var result = await SendCommandAsync(requeststring);

                if (result.Success && !string.IsNullOrEmpty(result.Response)) {

                    var bytes = result.Response.ParseDTByteString(blockSize * 4).HexStringToByteArray();

                    if (bytes == null) return null;

                    if (flipBytes) {
                        byteList.AddRange(bytes.BigToMixedEndian().Take(count).ToArray());
                    } else {
                        byteList.AddRange(bytes.Take(count).ToArray());
                    }

                }

                if (onProgress != null)
                    onProgress((double)i / wordLength);

            }

            return byteList.ToArray();

        }

        #endregion

        #region Helpers

        internal string GetStationNumber() {

            if (StationNumber != 0xEE && StationNumber > 99)
                throw new NotSupportedException("Station number was greater 99");

            if(StationNumber == 0xEE) return "EE";

            return StationNumber.ToString().PadLeft(2, '0');

        }

        #endregion

    }

}
