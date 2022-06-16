using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MewtocolNet.Responses;
using System.Linq;
using System.Globalization;

namespace MewtocolNet {
    
    public partial class MewtocolInterface {

        #region High level command handling

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
                    CpuInformation = PLCInfo.CpuInfo.BuildFromHexString(cpu, version, capacity),
                    OperationMode = PLCInfo.PLCMode.BuildFromHex(operation),
                    ErrorCode = error,
                    StationNumber = int.Parse(station ?? "0"),
                };
                return retInfo;

            } 
            return null;
        }

        /// <summary>
        /// Reads bool values from the plc by the given <c>Contact</c> List
        /// </summary>
        /// <param name="_contactsToRead">A list of contacts</param>
        /// <param name="_stationNumber">The PLCs station number</param>
        /// <returns>List of IBoolContact with unique copys of the given contacts</returns>
        public async Task<IEnumerable<IBoolContact>> ReadBoolContacts (List<Contact> _contactsToRead, int _stationNumber = 1) {
            
            //re order by contact pfx for faster querying 
            _contactsToRead = _contactsToRead.OrderBy(x=>x.Prefix).ToList();

            //return list
            List<IBoolContact> returnContacts = new List<IBoolContact>();

            //grouped by 8 each
            List<List<Contact>> nestedContacts = new List<List<Contact>>();

            //group into max 8 contacts list
            List<Contact> tempGroup = new List<Contact>();
            for (int i = 0; i < _contactsToRead.Count; i++) {
                tempGroup.Add(_contactsToRead[i]);
                //each 8 contacts make a new list
                if(i % 7 == 0 && i != 0 && i != _contactsToRead.Count) {
                    nestedContacts.Add(tempGroup);
                    tempGroup = new List<Contact>();
                }
                //if end of list and contacts cannot be broke down to 8 each group
                if(i == _contactsToRead.Count - 1 && _contactsToRead.Count % 8 != 0) {
                    nestedContacts.Add(tempGroup);
                    tempGroup = new List<Contact>();
                }
            }        

            //make task for each group
            foreach (var group in nestedContacts) {
                //regex for getting values
                StringBuilder regexString = new StringBuilder(@"\%..\$RC");
                //append start %01#RCP2
                StringBuilder messageString = new StringBuilder();
                messageString.Append($"%{_stationNumber.ToString().PadLeft(2, '0')}#RCP");
                messageString.Append($"{group.Count}");
                //append each contact of group Y0000 Y0001 etc
                foreach (var cont in group) {
                    messageString.Append(cont.BuildMewtocolIdent());
                    regexString.Append(@"([0-9])");
                }
                regexString.Append(@"(..)");
                //parse the result
                var result = await SendCommandAsync(messageString.ToString());
                Regex regCheck = new Regex(regexString.ToString(), RegexOptions.IgnoreCase);
                if(result.Success && regCheck.IsMatch(result.Response)) {
                    //parse result string
                    Match regMatch = regCheck.Match(result.Response);
                    // add to return list
                    for (int i = 0; i < group.Count; i++) {
                        Contact cont = group[i].ShallowCopy();
                        Contact toadd = cont;
                        if( regMatch.Groups[i + 1].Value == "1" ) {
                            toadd.Value = true;
                        } else if( regMatch.Groups[i + 1].Value == "0" ) {
                            toadd.Value = false;
                        }
                        returnContacts.Add(toadd);
                    }
                }
            }
            return returnContacts;            
        }

        /// <summary>
        /// Writes a boolen value to the given contact
        /// </summary>
        /// <param name="_contact">The contact to write</param>
        /// <param name="_value">The boolean state to write</param>
        /// <param name="_stationNumber">Station Number (optional)</param>
        /// <returns>A result struct</returns>
        public async Task<CommandResult> WriteContact (Contact _contact, bool _value, int _stationNumber = 1) {
            string stationNum = _stationNumber.ToString().PadLeft(2, '0');
            string dataArea = _contact.BuildMewtocolIdent();
            string dataString = _value ? "1" : "0";
            string requeststring = $"%{stationNum}#WCS{dataArea}{dataString}";
            var res = await SendCommandAsync(requeststring);
            return res;
        }

        /// <summary>
        /// Reads the given numeric register from PLC
        /// </summary>
        /// <typeparam name="T">Type of number (short, ushort, int, uint, float)</typeparam>
        /// <param name="_toRead">The register to read</param>
        /// <param name="_stationNumber">Station number to access</param>
        /// <returns>A result with the given NumberRegister containing the readback value and a result struct</returns>
        public async Task<NRegisterResult<T>> ReadNumRegister<T> (NRegister<T> _toRead, int _stationNumber = 1) {

            Type numType = typeof(T);

            string requeststring = $"%{_stationNumber.ToString().PadLeft(2, '0')}#RD{_toRead.BuildMewtocolIdent()}";
            var result = await SendCommandAsync(requeststring);

            if (numType == typeof(short)) {

                var resultBytes = result.Response.ParseDTByteString(4);
                var val = short.Parse(resultBytes, NumberStyles.HexNumber);
                (_toRead as NRegister<short>).LastValue = val;

            } else if (numType == typeof(ushort)) {
                var resultBytes = result.Response.ParseDTBytes(4);
                var val = BitConverter.ToInt16(resultBytes);
                _toRead.Value = (T)Convert.ChangeType(val, typeof(T));
            } else if (numType == typeof(int)) {
                var resultBytes = result.Response.ParseDTBytes(8);
                var val = BitConverter.ToInt16(resultBytes);
                _toRead.Value = (T)Convert.ChangeType(val, typeof(T));
            } else if (numType == typeof(uint)) {
                var resultBytes = result.Response.ParseDTBytes(8);
                var val = BitConverter.ToInt16(resultBytes);
                _toRead.Value = (T)Convert.ChangeType(val, typeof(T));
            } else if (numType == typeof(float)) {
                var resultBytes = result.Response.ParseDTBytes(8);
                var val = BitConverter.ToSingle(resultBytes);
                _toRead.Value = (T)Convert.ChangeType(val, typeof(T));
            }

            var finalRes = new NRegisterResult<T> {
                Result = result,
                Register = _toRead
            };

            return finalRes;
        }

        /// <summary>
        /// Reads the given numeric register from PLC
        /// </summary>
        /// <typeparam name="T">Type of number (short, ushort, int, uint, float)</typeparam>
        /// <param name="_toWrite">The register to write</param>
        /// <param name="_stationNumber">Station number to access</param>
        /// <returns>A result with the given NumberRegister and a result struct</returns>
        public async Task<NRegisterResult<T>> WriteNumRegister<T>(NRegister<T> _toWrite, T _value, int _stationNumber = 1) {

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
                toWriteVal = BitConverter.GetBytes(Convert.ToUInt32(_value));
            } else {
                toWriteVal = null;
            }

            string requeststring = $"%{_stationNumber.ToString().PadLeft(2, '0')}#WD{_toWrite.BuildMewtocolIdent()}{toWriteVal.ToHexString()}";
            var result = await SendCommandAsync(requeststring);

            return new NRegisterResult<T> {
                Result = result,
                Register = _toWrite
            };
        }


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

        public async Task<SRegisterResult> WriteStringRegister(SRegister _toWrite, string _value, int _stationNumber = 1) {

            if (_value == null) _value = "";
            if(_value.Length > _toWrite.ReservedSize) {
                throw new ArgumentException("Write string size cannot be longer than reserved string size");
            }

            string stationNum = _stationNumber.ToString().PadLeft(2, '0');
            string dataArea = _toWrite.BuildMewtocolIdent();
            string dataString = _value.BuildDTString(_toWrite.ReservedSize);
            string requeststring = $"%{stationNum}#WD{dataArea}{dataString}";

            Console.WriteLine($"reserved: {_toWrite.MemoryLength}, size: {_value.Length}");

            var result = await SendCommandAsync(requeststring);
            return new SRegisterResult {
                Result = result,
                Register = _toWrite
            };
        }

        #endregion

    }

}
