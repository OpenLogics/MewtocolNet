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

    public partial class MewtocolInterface {

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

        /// <summary>
        /// Changes the PLCs operation mode to the given one
        /// </summary>
        /// <param name="mode">The mode to change to</param>
        /// <returns>The success state of the write operation</returns>
        public async Task<bool> SetOperationMode (bool setRun) {

            string modeChar = setRun ? "R" : "P";

            string requeststring = $"%{GetStationNumber()}#RM{modeChar}";
            var result = await SendCommandAsync(requeststring);

            if (result.Success) {
                Logger.Log($"operation mode was changed to {(setRun ? "Run" : "Prog")}", LogLevel.Info, this);
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
        public async Task<bool> WriteByteRange (int start, byte[] byteArr) {

            string byteString = byteArr.BigToMixedEndian().ToHexString();
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
        /// Reads the bytes from the start adress for counts byte length
        /// </summary>
        /// <param name="start">Start adress</param>
        /// <param name="count">Number of bytes to get</param>
        /// <param name="flipBytes">Flips bytes from big to mixed endian</param>
        /// <param name="onProgress">Gets invoked when the progress changes, contains the progress as a double</param>
        /// <returns>A byte array or null of there was an error</returns>
        public async Task<byte[]> ReadByteRange(int start, int count, bool flipBytes = true, Action<double> onProgress = null) {

            var byteList = new List<byte>();

            var wordLength = count / 2;
            if (count % 2 != 0)
                wordLength++;


            //read blocks of max 4 words per msg
            for (int i = 0; i < wordLength; i += 8) {

                int curWordStart = start + i;
                int curWordEnd = curWordStart + 7;

                string startStr = curWordStart.ToString().PadLeft(5, '0');
                string endStr = (curWordEnd).ToString().PadLeft(5, '0');

                string requeststring = $"%{GetStationNumber()}#RDD{startStr}{endStr}";
                var result = await SendCommandAsync(requeststring);

                if (result.Success && !string.IsNullOrEmpty(result.Response)) {

                    var bytes = result.Response.ParseDTByteString(8 * 4).HexStringToByteArray();

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

        #region Raw register reading / writing

        internal async Task<byte[]> ReadRawRegisterAsync (IRegisterInternal _toRead) {

            var toreadType = _toRead.GetType();

            //returns a byte array 1 long and with the byte beeing 0 or 1
            if (toreadType == typeof(BoolRegister)) {

                string requeststring = $"%{GetStationNumber()}#RCS{_toRead.BuildMewtocolQuery()}";
                var result = await SendCommandAsync(requeststring);
                if (!result.Success) return null;

                var resultBool = result.Response.ParseRCSingleBit();
                if (resultBool != null) {

                    return resultBool.Value ? new byte[] { 1 } : new byte[] { 0 };

                }

            }

            //returns a byte array 2 bytes or 4 bytes long depending on the data size
            if (toreadType.IsGenericType && _toRead.GetType().GetGenericTypeDefinition() == typeof(NumberRegister<>)) {

                string requeststring = $"%{GetStationNumber()}#RD{_toRead.BuildMewtocolQuery()}";
                var result = await SendCommandAsync(requeststring);
                if (!result.Success) return null;

                if(_toRead.RegisterType == RegisterType.DT) {

                    return result.Response.ParseDTByteString(4).HexStringToByteArray();

                } else {

                    return result.Response.ParseDTByteString(8).HexStringToByteArray();

                }

            }

            //returns a byte array with variable size
            if (toreadType == typeof(BytesRegister)) {

                string requeststring = $"%{GetStationNumber()}#RD{_toRead.BuildMewtocolQuery()}";
                var result = await SendCommandAsync(requeststring);
                if (!result.Success) return null;

                var resBytes = result.Response.ParseDTRawStringAsBytes();

                return resBytes;

            }

            if (toreadType == typeof(StringRegister)) {

                string requeststring = $"%{GetStationNumber()}#RD{_toRead.BuildMewtocolQuery()}";
                var result = await SendCommandAsync(requeststring);
                if (!result.Success) return null;

                var resBytes = result.Response.ParseDTRawStringAsBytes();

                return resBytes;

            }

            throw new Exception($"Failed to load the byte data for: {_toRead}");

        }

        internal async Task<bool> WriteRawRegisterAsync (IRegisterInternal _toWrite, byte[] data) {

            var toWriteType = _toWrite.GetType();

            //returns a byte array 1 long and with the byte beeing 0 or 1
            if (toWriteType == typeof(BoolRegister)) {

                string requeststring = $"%{GetStationNumber()}#WCS{_toWrite.BuildMewtocolQuery()}{(data[0] == 1 ? "1" : "0")}";
                var result = await SendCommandAsync(requeststring);
                return result.Success;

            }

            //writes a byte array 2 bytes or 4 bytes long depending on the data size
            if (toWriteType.IsGenericType && toWriteType.GetGenericTypeDefinition() == typeof(NumberRegister<>)) {

                string requeststring = $"%{GetStationNumber()}#WD{_toWrite.BuildMewtocolQuery()}{data.ToHexString()}";
                var result = await SendCommandAsync(requeststring);
                return result.Success;

            }

            //returns a byte array with variable size
            if (toWriteType == typeof(BytesRegister)) {

                throw new NotImplementedException("Not imp");

            }

            //writes to the string area
            if (toWriteType == typeof(StringRegister)) {

                string requeststring = $"%{GetStationNumber()}#WD{_toWrite.BuildMewtocolQuery()}{data.ToHexString()}";
                var result = await SendCommandAsync(requeststring);
                return result.Success;

            }

            return false;

        }

        #endregion

        #region Register reading / writing

        internal async Task<bool> SetRegisterAsync (IRegister register, object value) {

            var internalReg = (IRegisterInternal)register;

            return await internalReg.WriteAsync(value);

        }

        #endregion

        #region Reading / Writing Plc program

        public async Task GetSystemRegister () {

            //the "." means CR or \r

            await SendCommandAsync("%EE#RT");

            //then get plc status extended? gets polled all time
            // %EE#EX00RT00
            await SendCommandAsync("%EE#EX00RT00");



        }

        #endregion

        #region Helpers

        internal string GetStationNumber() {

            return StationNumber.ToString().PadLeft(2, '0');


        }

        #endregion

    }

}
