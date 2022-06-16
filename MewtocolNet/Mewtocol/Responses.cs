using System; 
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MewtocolNet.Responses {
    
    /// <summary>
    /// The formatted result of a ascii command
    /// </summary>
    public struct CommandResult {
        public bool Success {get;set;}
        public string Response {get;set;}
        public string Error {get;set;}
        public string ErrorDescription {get;set;}

        public override string ToString() {
            string errmsg = Success ? "" : ErrorDescription;
            return $"Success: {Success}, Response: {Response} {errmsg}";
        }
    }

  

    /// <summary>
    /// Contains generic information about the plc
    /// </summary>
    public class PLCInfo {

        public class PLCMode {
            public bool RunMode {get;set;}
            public bool TestRunMode {get;set;}
            public bool BreakExcecuting {get;set;}
            public bool BreakValid {get;set;}
            public bool OutputEnabled {get;set;}
            public bool StepRunMode {get;set;}
            public bool MessageExecuting {get;set;}
            public bool RemoteMode {get;set;}

            /// <summary>
            /// Gets operation mode from 2 digit hex number
            /// </summary>
            public static PLCMode BuildFromHex (string _hexString) {

                string lower = Convert.ToString(Convert.ToInt32(_hexString.Substring(0, 1)), 2).PadLeft(4, '0');
                string higher = Convert.ToString(Convert.ToInt32(_hexString.Substring(1, 1)), 2).PadLeft(4, '0');
                string combined = lower + higher;

                var retMode = new PLCMode();

                for (int i = 0; i < 8; i++) {
                    char digit = combined[i];
                    bool state = false;
                    if(digit.ToString() == "1") state = true; 
                    switch (i) {
                        case 0 : 
                        retMode.RunMode = state;
                        break;
                        case 1 : 
                        retMode.TestRunMode = state;
                        break;
                        case 2 : 
                        retMode.BreakExcecuting = state;
                        break;
                        case 3 : 
                        retMode.BreakValid = state;
                        break;
                        case 4 : 
                        retMode.OutputEnabled = state;
                        break;
                        case 5 : 
                        retMode.StepRunMode = state;
                        break;
                        case 6 : 
                        retMode.MessageExecuting = state;
                        break;
                        case 7 : 
                        retMode.RemoteMode = state;
                        break;
                    }
                }

                return retMode;

            }
        }

        public class CpuInfo {
            public enum CpuType {
                FP0_FP1_2_7K,
                FP0_FP1_5K_10K,
                FP1_M_0_9K,
                FP2_16K_32K,
                FP3_C_10K,
                FP3_C_16K,
                FP5_16K,
                FP5_24K,
                FP_Sigma_X_H_30K_60K_120K

            }

            public CpuType Cputype {get;set;}
            public int ProgramCapacity {get;set;}
            public string CpuVersion {get;set;}


            public static CpuInfo BuildFromHexString (string _cpuType, string _cpuVersion, string _progCapacity) {

                CpuInfo retInf = new CpuInfo();

                switch (_cpuType) {
                    case "02":
                    retInf.Cputype = CpuType.FP5_16K;
                    break;
                    case "03":
                    retInf.Cputype = CpuType.FP3_C_10K;
                    break;
                    case "04":
                    retInf.Cputype = CpuType.FP1_M_0_9K;
                    break;
                    case "05":
                    retInf.Cputype = CpuType.FP0_FP1_2_7K;
                    break;
                    case "06":
                    retInf.Cputype = CpuType.FP0_FP1_5K_10K;
                    break;
                    case "12":
                    retInf.Cputype = CpuType.FP5_24K;
                    break;
                    case "13":
                    retInf.Cputype = CpuType.FP3_C_16K;
                    break;
                    case "20":
                    retInf.Cputype = CpuType.FP_Sigma_X_H_30K_60K_120K;
                    break;
                    case "50":
                    retInf.Cputype = CpuType.FP2_16K_32K;
                    break;
                }

                retInf.ProgramCapacity = Convert.ToInt32(_progCapacity);
                retInf.CpuVersion = _cpuVersion.Insert(1, ".");
                return retInf;

            }
        }

        public CpuInfo CpuInformation {get;set;}
        public PLCMode OperationMode {get;set;}
        public string ErrorCode {get;set;}
        public int StationNumber { get;set;}        

        public override string ToString () {

            return $"Type: {CpuInformation.Cputype},\n" +
                   $"Capacity: {CpuInformation.ProgramCapacity}k\n" +
                   $"CPU v: {CpuInformation.CpuVersion}\n" +
                   $"Station Num: {StationNumber}\n" +
                   $"--------------------------------\n" +
                   $"OP Mode: {(OperationMode.RunMode ? "Run" : "Prog")}\n" +
                   $"Error Code: {ErrorCode}";

        }

    }



    /// <summary>
    /// Contact as bool contact
    /// </summary>
    public interface IBoolContact {
        public string Name {get;set;}
        public string Identifier {get;}
        public bool? Value {get;set;}

    }

    /// <summary>
    /// A class describing a PLC contact
    /// </summary>
    public class Contact : IBoolContact {
        public string Name {get;set;}
        public PFX Prefix {get;set;}
        public int Number {get;set;}
        public ContactType Type {get;set;}
        public string Endprefix {get;set;}
        public string Asciistring {get => BuildMewtocolIdent();}
        public string Identifier {get => Asciistring;}
        public bool? Value {get;set;} = null;
        public enum ContactType {
            Unknown,
            Input,
            Output,
        }

        public enum PFX {
            X,
            Y,
            R
        }

        /// <summary>
        /// Creates a new base Contact
        /// </summary>
        /// <param name="_prefix">A prefix identifier eg. X,Y,R,L</param>
        /// <param name="_number">The number of the PLC contact</param>
        public Contact (PFX _prefix, int _number, string _name = "unknown") {
            switch (_prefix) {
                case PFX.X:
                Type = ContactType.Input;
                break;
                case PFX.Y:
                case PFX.R:
                Type = ContactType.Output;
                break;
            }

            Prefix = _prefix;
            Number = _number;
            Name = _name;
        }

        /// <summary>
        /// Creates a new base Contact
        /// </summary>
        /// <param name="_prefix">A prefix identifier eg. X,Y,R,L</param>
        /// <param name="_number">The number of the PLC contact</param>
        public Contact (string _prefix, int _number, string _name = "unknown") {
            PFX parsedPFX;
            if(Enum.TryParse<PFX>(_prefix, true, out parsedPFX)) {
                switch (parsedPFX) {
                    case PFX.X:
                    Type = ContactType.Input;
                    break;
                    case PFX.Y:
                    case PFX.R:
                    Type = ContactType.Output;
                    break;
                }
                Prefix = parsedPFX;
                Number = _number;
                Name = _name;
            } else {
                throw new ArgumentException($"The prefix {_prefix} is no valid contact prefix");
            } 
        }

        /// <summary>
        /// Build contact from complete contact name
        /// </summary>
        /// <param name="_contactName">Complete contact name e.g. Y1C, Y3D or X1</param>
        public Contact (string _contactName, string _name = "unknown") {
            
            string prefix = "";
            int number = 0;
            string endpfx = null;

            Match regcheck = new Regex(@"(Y|X|R|L)([0-9]{1,3})?(.)?", RegexOptions.IgnoreCase).Match(_contactName);
            if(regcheck.Success) {

                /* for (int i = 0; i < regcheck.Groups.Count; i++) {
                    var item = regcheck.Groups[i].Value;
                    Console.WriteLine(item);
                } */

                prefix = regcheck.Groups[1].Value;
                number = regcheck.Groups[2]?.Value != string.Empty ? Convert.ToInt32(regcheck.Groups[2].Value) : -1;
                endpfx = regcheck.Groups[3]?.Value;
            } else {
                throw new ArgumentException($"The contact {_contactName} is no valid contact");
            }
            
            PFX parsedPFX;
            if(Enum.TryParse<PFX>(prefix, true, out parsedPFX)) {
                switch (parsedPFX) {
                    case PFX.X:
                    Type = ContactType.Input;
                    break;
                    case PFX.Y:
                    case PFX.R:
                    Type = ContactType.Output;
                    break;
                }
                Prefix = parsedPFX;
                Number = number;
                Endprefix = endpfx;
                Name = _name;

            } else {
                throw new ArgumentException($"The prefix {prefix} is no valid contact prefix");
            } 

            Console.WriteLine(BuildMewtocolIdent());
        }

        /// <summary>
        /// Builds the mewtocol ascii contact identifier
        /// </summary>
        /// <returns>The identifier e.g. Y0001 or Y000A or X001C</returns>
        public string BuildMewtocolIdent () {
            string contactstring = "";
            if(Endprefix == null) {
                contactstring += Prefix;
                contactstring += Number.ToString().PadLeft(4, '0');
            } else {
                contactstring += Prefix;
                if(Number == -1) {
                    contactstring += "000" + Endprefix;
                } else {
                    contactstring += (Number.ToString() + Endprefix).PadLeft(4, '0');
                }
            }
            if(string.IsNullOrEmpty(contactstring)) {
                return null;
            }
            return contactstring;
        }

        /// <summary>
        /// Converts the class to a generic json compatible object
        /// </summary>
        /// <returns></returns>
        public object ToGenericObject () {
            return new {
                Name = this.Name,
                Identifier = this.Asciistring,
                Value = this.Value
            };
        }
        /// <summary>
        /// Creates a copy of the contact
        /// </summary>
        public Contact ShallowCopy() {
            return (Contact) this.MemberwiseClone();
        }

    }
   

}