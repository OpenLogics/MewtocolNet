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
        public async Task<PLCInfo> GetPLCInfoAsync() {
            var resu = await SendCommandAsync("%01#RT");
            if (!resu.Success) return null;

            var reg = new Regex(@"\%([0-9]{2})\$RT([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{4})..", RegexOptions.IgnoreCase);
            Match m = reg.Match(resu.Response);

            if (m.Success) {

                string station = m.Groups[1].Value;
                string cpu = m.Groups[2].Value;
                string version = m.Groups[3].Value;
                string capacity = m.Groups[4].Value;
                string operation = m.Groups[5].Value;

                string errorflag = m.Groups[7].Value;
                string error = m.Groups[8].Value;

                PLCInfo retInfo = new PLCInfo {
                    CpuInformation = CpuInfo.BuildFromHexString(cpu, version, capacity),
                    OperationMode = PLCMode.BuildFromHex(operation),
                    ErrorCode = error,
                    StationNumber = int.Parse(station ?? "0"),
                };

                PlcInfo = retInfo;
                return retInfo;

            }
            return null;
        }

        #endregion

        #region Operation mode changing 

        /// <summary>
        /// Changes the PLCs operation mode to the given one
        /// </summary>
        /// <param name="mode">The mode to change to</param>
        /// <returns>The success state of the write operation</returns>
        public async Task<bool> SetOperationMode(OPMode mode) {

            string modeChar = mode == OPMode.Prog ? "P" : "R";

            string requeststring = $"%{GetStationNumber()}#RM{modeChar}";
            var result = await SendCommandAsync(requeststring);

            if (result.Success) {
                Logger.Log($"operation mode was changed to {mode}", LogLevel.Info, this);
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
        public async Task<bool> WriteByteRange(int start, byte[] byteArr) {

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
        /// <param name="onProgress">Gets invoked when the progress changes, contains the progress as a double</param>
        /// <returns>A byte array or null of there was an error</returns>
        public async Task<byte[]> ReadByteRange(int start, int count, Action<double> onProgress = null) {

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

                    if (bytes == null) {
                        return null;
                    }

                    byteList.AddRange(bytes.BigToMixedEndian().Take(count).ToArray());

                }

                if (onProgress != null)
                    onProgress((double)i / wordLength);

            }

            return byteList.ToArray();

        }

        #endregion

        #region Raw register reading / writing

        internal async Task<byte[]> ReadRawRegisterAsync (IRegister _toRead) {

            //returns a byte array 1 long and with the byte beeing 0 or 1
            if (_toRead.GetType() == typeof(BoolRegister)) {

                string requeststring = $"%{GetStationNumber()}#RCS{_toRead.BuildMewtocolQuery()}";
                var result = await SendCommandAsync(requeststring);

                var resultBool = result.Response.ParseRCSingleBit();
                if (resultBool != null) {

                    return resultBool.Value ? new byte[] { 1 } : new byte[] { 0 };

                }

            }

            //returns a byte array 2 bytes or 4 bytes long depending on the data size
            if (_toRead.GetType().GetGenericTypeDefinition() == typeof(NumberRegister<>)) {

                string requeststring = $"%{GetStationNumber()}#RD{_toRead.BuildMewtocolQuery()}";
                var result = await SendCommandAsync(requeststring);

                if (!result.Success)
                    throw new Exception($"Failed to load the byte data for: {_toRead}");

                if(_toRead.RegisterType == RegisterType.DT) {

                    return result.Response.ParseDTByteString(4).HexStringToByteArray();

                } else {

                    return result.Response.ParseDTByteString(8).HexStringToByteArray();

                }

            }

            //returns a byte array with variable size
            if (_toRead.GetType() == typeof(BytesRegister<>)) {

                string requeststring = $"%{GetStationNumber()}#RD{_toRead.BuildMewtocolQuery()}";
                var result = await SendCommandAsync(requeststring);

                if (!result.Success)
                    throw new Exception($"Failed to load the byte data for: {_toRead}");

                return result.Response.ParseDTString().ReverseByteOrder().HexStringToByteArray();

            }

            throw new Exception($"Failed to load the byte data for: {_toRead}");

        }

        internal async Task<bool> WriteRawRegisterAsync (IRegister _toWrite, byte[] data) {

            //returns a byte array 1 long and with the byte beeing 0 or 1
            if (_toWrite.GetType() == typeof(BoolRegister)) {

                string requeststring = $"%{GetStationNumber()}#WCS{_toWrite.BuildMewtocolQuery()}{(data[0] == 1 ? "1" : "0")}";
                var result = await SendCommandAsync(requeststring);
                return result.Success;

            }

            //returns a byte array 2 bytes or 4 bytes long depending on the data size
            if (_toWrite.GetType().GetGenericTypeDefinition() == typeof(NumberRegister<>)) {

                string requeststring = $"%{GetStationNumber()}#WD{_toWrite.BuildMewtocolQuery()}{data.ToHexString()}";
                var result = await SendCommandAsync(requeststring);
                return result.Success;

            }

            //returns a byte array with variable size
            if (_toWrite.GetType() == typeof(BytesRegister<>)) {

                //string stationNum = GetStationNumber();
                //string dataString = gotBytes.BuildDTString(_toWrite.ReservedSize);
                //string dataArea = _toWrite.BuildCustomIdent(dataString.Length / 4);

                //string requeststring = $"%{stationNum}#WD{dataArea}{dataString}";

                //var result = await SendCommandAsync(requeststring);

            }

            return false;

        }

        #endregion

        #region Register reading / writing

        public async Task<bool> SetRegisterAsync (IRegister register, object value) {

            var internalReg = (IRegisterInternal)register;

            return await internalReg.WriteAsync(this, value);

        }

        #endregion

        #region Helpers

        internal string GetStationNumber() {

            return StationNumber.ToString().PadLeft(2, '0');


        }

        #endregion

    }

}
