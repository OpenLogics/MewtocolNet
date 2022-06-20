using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MewtocolNet.Registers;
using System.Linq;
using System.Globalization;

namespace MewtocolNet {
    
    public partial class MewtocolInterface {

        #region PLC info getters

        /// <summary>
        /// Gets generic information about the PLC
        /// </summary>
        /// <returns>A PLCInfo class</returns>
        public async Task<PLCInfo> GetPLCInfoAsync () {
            var resu = await SendCommandAsync("%01#RT");
            if(!resu.Success) return null;

            var reg = new Regex(@"\%([0-9]{2})\$RT([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{2})([0-9]{4})..", RegexOptions.IgnoreCase);
            Match m = reg.Match(resu.Response);
            
            if(m.Success) {

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
                return retInfo;

            } 
            return null;
        }

        #endregion

        #region Bool register reading / writing

        /// <summary>
        /// Reads the given boolean register from the PLC
        /// </summary>
        /// <param name="_toRead">The register to read</param>
        /// <param name="_stationNumber">Station number to access</param>
        public async Task<BRegisterResult> ReadBoolRegister (BRegister _toRead, int _stationNumber = 1) {

            string requeststring = $"%{_stationNumber.ToString().PadLeft(2, '0')}#RCS{_toRead.BuildMewtocolIdent()}";
            var result = await SendCommandAsync(requeststring);

            if(!result.Success) {
                return new BRegisterResult {
                    Result = result,
                    Register = _toRead
                };
            }

            var resultBool = result.Response.ParseRCSingleBit();
            if(resultBool != null) {
                _toRead.LastValue = resultBool.Value;
            } 

            var finalRes = new BRegisterResult {
                Result = result,
                Register = _toRead
            };

            return finalRes;

        }

        /// <summary>
        /// Writes to the given bool register on the PLC
        /// </summary>
        /// <param name="_toWrite">The register to write to</param>
        /// <param name="_stationNumber">Station number to access</param>
        /// <returns>The success state of the write operation</returns>
        public async Task<bool> WriteBoolRegister (BRegister _toWrite, bool value, int _stationNumber = 1) {

            string requeststring = $"%{_stationNumber.ToString().PadLeft(2, '0')}#WCS{_toWrite.BuildMewtocolIdent()}{(value ? "1" : "0")}";

            var result = await SendCommandAsync(requeststring);

            return result.Success && result.Response.StartsWith($"%{ _stationNumber.ToString().PadLeft(2, '0')}#WC");

        }

        #endregion

        #region Number register reading / writing

        /// <summary>
        /// Reads the given numeric register from the PLC
        /// </summary>
        /// <typeparam name="T">Type of number (short, ushort, int, uint, float)</typeparam>
        /// <param name="_toRead">The register to read</param>
        /// <param name="_stationNumber">Station number to access</param>
        /// <returns>A result with the given NumberRegister containing the readback value and a result struct</returns>
        public async Task<NRegisterResult<T>> ReadNumRegister<T> (NRegister<T> _toRead, int _stationNumber = 1) {

            Type numType = typeof(T);

            string requeststring = $"%{_stationNumber.ToString().PadLeft(2, '0')}#RD{_toRead.BuildMewtocolIdent()}";
            var result = await SendCommandAsync(requeststring);

            if(!result.Success || string.IsNullOrEmpty(result.Response)) {
                return new NRegisterResult<T> {
                    Result = result,
                    Register = _toRead
                };
            }
                
            if (numType == typeof(short)) {

                var resultBytes = result.Response.ParseDTByteString(4).ReverseByteOrder();
                var val = short.Parse(resultBytes, NumberStyles.HexNumber);
                (_toRead as NRegister<short>).LastValue = val;

            } else if (numType == typeof(ushort)) {

                var resultBytes = result.Response.ParseDTByteString(4).ReverseByteOrder();
                var val = ushort.Parse(resultBytes, NumberStyles.HexNumber);
                (_toRead as NRegister<ushort>).LastValue = val;

            } else if (numType == typeof(int)) {

                var resultBytes = result.Response.ParseDTByteString(8).ReverseByteOrder();
                var val = int.Parse(resultBytes, NumberStyles.HexNumber);
                (_toRead as NRegister<int>).LastValue = val;

            } else if (numType == typeof(uint)) {

                var resultBytes = result.Response.ParseDTByteString(8).ReverseByteOrder();
                var val = uint.Parse(resultBytes, NumberStyles.HexNumber);
                (_toRead as NRegister<uint>).LastValue = val;

            } else if (numType == typeof(float)) {

                var resultBytes = result.Response.ParseDTByteString(8).ReverseByteOrder();
                //convert to unsigned int first
                var val = uint.Parse(resultBytes, NumberStyles.HexNumber);

                byte[] floatVals = BitConverter.GetBytes(val);
                float finalFloat = BitConverter.ToSingle(floatVals, 0);

                (_toRead as NRegister<float>).LastValue = finalFloat;

            } else if (numType == typeof(TimeSpan)) {

                var resultBytes = result.Response.ParseDTByteString(8).ReverseByteOrder();
                //convert to unsigned int first
                var vallong = long.Parse(resultBytes, NumberStyles.HexNumber);
                var valMillis = vallong * 10;
                var ts = TimeSpan.FromMilliseconds(valMillis);

                //minmax writable / readable value is 10ms
                (_toRead as NRegister<TimeSpan>).LastValue = ts;

            }

            var finalRes = new NRegisterResult<T> {
                Result = result,
                Register = _toRead
            };

            return finalRes;
        }

        /// <summary>
        /// Reads the given numeric register from the PLC
        /// </summary>
        /// <typeparam name="T">Type of number (short, ushort, int, uint, float)</typeparam>
        /// <param name="_toWrite">The register to write</param>
        /// <param name="_stationNumber">Station number to access</param>
        /// <returns>The success state of the write operation</returns>
        public async Task<bool> WriteNumRegister<T> (NRegister<T> _toWrite, T _value, int _stationNumber = 1) {

            byte[] toWriteVal;
            Type numType = typeof(T);

            if (numType == typeof(short)) {
                toWriteVal = BitConverter.GetBytes(Convert.ToInt16(_value));
            } else if (numType == typeof(ushort)) {
                toWriteVal = BitConverter.GetBytes(Convert.ToUInt16(_value));
            } else if (numType == typeof(int)) {
                toWriteVal = BitConverter.GetBytes(Convert.ToInt32(_value));
            } else if (numType == typeof(uint)) {
                toWriteVal = BitConverter.GetBytes(Convert.ToUInt32(_value));
            } else if (numType == typeof(float)) {

                var fl = _value as float?;
                if (fl == null)
                    throw new NullReferenceException("Float cannot be null");

                toWriteVal = BitConverter.GetBytes(fl.Value);

            } else if (numType == typeof(TimeSpan)) {

                var fl = _value as TimeSpan?;
                if (fl == null)
                    throw new NullReferenceException("Timespan cannot be null");

                var tLong = (uint)(fl.Value.TotalMilliseconds / 10);
                toWriteVal = BitConverter.GetBytes(tLong);

            } else {
                toWriteVal = null;
            }

            string requeststring = $"%{_stationNumber.ToString().PadLeft(2, '0')}#WD{_toWrite.BuildMewtocolIdent()}{toWriteVal.ToHexString()}";

            var result = await SendCommandAsync(requeststring);

            return result.Success && result.Response.StartsWith($"%{ _stationNumber.ToString().PadLeft(2, '0')}#WD");

        }

        #endregion

        #region String register reading / writing

        //string is build up like this
        //04 00 04 00 53 50 33 35 13
        //0, 1 = reserved size
        //1, 2 = current size
        //3,4,5,6 = ASCII encoded chars (SP35)
        //7,8 = checksum

        /// <summary>
        /// Reads back the value of a string register
        /// </summary>
        /// <param name="_toRead">The register to read</param>
        /// <param name="_stationNumber">The station number of the PLC</param>
        /// <returns></returns>
        public async Task<SRegisterResult> ReadStringRegister (SRegister _toRead, int _stationNumber = 1) {

            string requeststring = $"%{_stationNumber.ToString().PadLeft(2, '0')}#RD{_toRead.BuildMewtocolIdent()}";
            var result = await SendCommandAsync(requeststring);
            if (result.Success)
                _toRead.SetValueFromPLC(result.Response.ParseDTString());
            return new SRegisterResult {
                Result = result,
                Register = _toRead
            };
        }

        /// <summary>
        /// Writes a string to a string register
        /// </summary>
        /// <param name="_toWrite">The register to write</param>
        /// <param name="_value">The value to write, if the strings length is longer than the cap size it gets trimmed to the max char length</param>
        /// <param name="_stationNumber">The station number of the PLC</param>
        /// <returns>The success state of the write operation</returns>
        public async Task<bool> WriteStringRegister(SRegister _toWrite, string _value, int _stationNumber = 1) {

            if (_value == null) _value = "";
            if(_value.Length > _toWrite.ReservedSize) {
                throw new ArgumentException("Write string size cannot be longer than reserved string size");
            }

            string stationNum = _stationNumber.ToString().PadLeft(2, '0');
            string dataString = _value.BuildDTString(_toWrite.ReservedSize);
            string dataArea = _toWrite.BuildCustomIdent(dataString.Length / 4);
            
            string requeststring = $"%{stationNum}#WD{dataArea}{dataString}";

            var result = await SendCommandAsync(requeststring);


            return result.Success && result.Response.StartsWith($"%{ _stationNumber.ToString().PadLeft(2, '0')}#WD");
        }

        #endregion

    }

}
