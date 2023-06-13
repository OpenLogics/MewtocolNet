using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// A class describing a register
    /// </summary>
    public abstract class Register : INotifyPropertyChanged {

        /// <summary>
        /// Gets called whenever the value was changed
        /// </summary>
        public event Action<object> ValueChanged;
        /// <summary>
        /// Triggers when a property on the register changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        internal Type collectionType;
        /// <summary>
        /// The type of collection the register is in or null of added manually
        /// </summary>
        public Type CollectionType => collectionType;

        internal string name;
        /// <summary>
        /// The register name or null of not defined
        /// </summary>
        public string Name => name;

        internal int memoryAdress;
        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public int MemoryAdress => memoryAdress;

        internal int memoryLength;
        /// <summary>
        /// The rgisters memory length
        /// </summary>
        public int MemoryLength => memoryLength;

        /// <summary>
        /// The value of the register auto converted to a string
        /// </summary>
        public string StringValue => GetValueString();

        /// <summary>
        /// The name the register would have in the PLC
        /// </summary>
        public string RegisterPLCName => GetRegisterPLCName();

        /// <summary>
        /// The combined name with the holding register class type infront
        /// </summary>
        public string CombinedName => GetCombinedName();

        /// <summary>
        /// The name of the class that contains this register or empty if it was added manually
        /// </summary>
        public string ContainerName => GetContainerName();

        internal bool isUsedBitwise { get; set; }    
        internal Type enumType { get; set; }

        internal Register () {
            ValueChanged += (obj) => {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StringValue)));
            };
        }

        /// <summary>
        /// Builds the register area name
        /// </summary>
        public virtual string BuildMewtocolIdent() {

            StringBuilder asciistring = new StringBuilder("D");
            asciistring.Append(MemoryAdress.ToString().PadLeft(5, '0'));
            asciistring.Append((MemoryAdress + MemoryLength).ToString().PadLeft(5, '0'));
            return asciistring.ToString();

        }
        internal void TriggerChangedEvnt(object changed) {
            ValueChanged?.Invoke(changed);
        }

        internal void TriggerNotifyChange () {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
        }

        internal void ClearValue () {

            if (enumType != null && this is NRegister<int> intEnumReg) {
                intEnumReg.SetValueFromPLC((int)0);
            }
            if (this is NRegister<short> shortReg) {
                shortReg.SetValueFromPLC((short)0);
            }
            if (this is NRegister<ushort> ushortReg) {
                ushortReg.SetValueFromPLC((ushort)0);
            }
            if (this is NRegister<int> intReg) {
                intReg.SetValueFromPLC((int)0);
            }
            if (this is NRegister<uint> uintReg) {
                uintReg.SetValueFromPLC((uint)0);
            }
            if (this is NRegister<float> floatReg) {
                floatReg.SetValueFromPLC((float)0);
            }
            if (this is NRegister<TimeSpan> tsReg) {
                tsReg.SetValueFromPLC(TimeSpan.Zero);
            }
            if (this is BRegister boolReg) {
                boolReg.SetValueFromPLC(false);
            }
            if (this is SRegister stringReg) {
                stringReg.SetValueFromPLC(null);
            }

        }

        /// <summary>
        /// Gets the starting memory are either numeric or A,B,C,D etc for special areas like inputs
        /// </summary>
        /// <returns></returns>
        public string GetStartingMemoryArea () {

            if (this is BRegister bReg && bReg.SpecialAddress != SpecialAddress.None) {
                return bReg.SpecialAddress.ToString();
            }

            return this.MemoryAdress.ToString();

        }

        /// <summary>
        /// Gets the current value in the adress as a string 
        /// </summary>
        /// <returns></returns>
        public string GetValueString () {

            if (enumType != null && this is NRegister<int> intEnumReg) {

                var dict = new Dictionary<int, string>();

                foreach (var name in Enum.GetNames(enumType)) {
                    int enumKey = (int)Enum.Parse(enumType, name);
                    if(!dict.ContainsKey(enumKey)) {
                        dict.Add(enumKey, name);
                    }
                }

                if(dict.ContainsKey(intEnumReg.Value)) {
                    return $"{intEnumReg.Value} ({dict[intEnumReg.Value]})";
                } else {
                    return $"{intEnumReg.Value} (Missing Enum)";
                }   

            }
            if (this is NRegister<short> shortReg) {
                return $"{shortReg.Value}{(isUsedBitwise ? $" [{shortReg.GetBitwise().ToBitString()}]" : "")}";
            }
            if (this is NRegister<ushort> ushortReg) {
                return ushortReg.Value.ToString();
            }
            if (this is NRegister<int> intReg) {
                return $"{intReg.Value}{(isUsedBitwise ? $" [{intReg.GetBitwise().ToBitString()}]" : "")}";
            }
            if (this is NRegister<uint> uintReg) {
                return uintReg.Value.ToString();
            }
            if (this is NRegister<float> floatReg) {
                return floatReg.Value.ToString();
            }
            if (this is NRegister<TimeSpan> tsReg) {
                return tsReg.Value.ToString();
            }
            if (this is BRegister boolReg) {
                return boolReg.Value.ToString();
            }
            if (this is SRegister stringReg) {
                return stringReg.Value ?? "";
            }

            return "Type of the register is not supported.";

        }

        /// <summary>
        /// Gets the register bitwise if its a 16 or 32 bit int
        /// </summary>
        /// <returns>A bitarray</returns>
        public BitArray GetBitwise () {

            if (this is NRegister<short> shortReg) {

                var bytes = BitConverter.GetBytes(shortReg.Value);
                BitArray bitAr = new BitArray(bytes);
                return bitAr;

            }

            if (this is NRegister<int> intReg) {

                var bytes = BitConverter.GetBytes(intReg.Value);
                BitArray bitAr = new BitArray(bytes);
                return bitAr;

            }

            return null;

        }

        /// <summary>
        /// Gets the register dataarea string DT for 16bit and DDT for 32 bit types
        /// </summary>
        public string GetRegisterString () {

            if (this is NRegister<short> shortReg) {
                return "DT";
            }
            if (this is NRegister<ushort> ushortReg) {
                return "DT";
            }
            if (this is NRegister<int> intReg) {
                return "DDT";
            }
            if (this is NRegister<uint> uintReg) {
                return "DDT";
            }
            if (this is NRegister<float> floatReg) {
                return "DDT";
            }
            if (this is NRegister<TimeSpan> tsReg) {
                return "DDT";
            }
            if (this is BRegister boolReg) {
                return boolReg.RegType.ToString();  
            }
            if (this is SRegister stringReg) {
                return "DT";

            }

            return "Type of the register is not supported.";

        }

        /// <summary>
        /// Builds a register from a given register string like DT100 / XA / Y1
        /// </summary>
        /// <param name="regString">The input string to parse</param>
        /// <returns>A built register</returns>
        public static Register FromString (string regString, string name = null) {

            var match = Regex.Match(regString, @"(X|Y|R)([A-F]|[0-9_.-]{1,5})");

            if (match != null && match.Success) {

                var typeGroup = match.Groups[1].Value;
                var areaGroup = match.Groups[2].Value;

                bool isBool = false;
                var parsedRegType = RegisterType.R;
                if (new string[] { "X", "Y", "R" }.Contains(typeGroup)) {
                    switch (typeGroup) {
                        case "X":
                        parsedRegType = RegisterType.X;
                        isBool = true;
                        break;
                        case "Y":
                        parsedRegType = RegisterType.Y;
                        isBool = true;
                        break;
                        case "R":
                        parsedRegType = RegisterType.R;
                        isBool = true;
                        break;
                    }
                }

                if(!isBool) {
                    throw new NotSupportedException($"Register with value {regString} is not of type bool");
                }
                     
                if (int.TryParse(areaGroup, out var parsedNum) && isBool) {

                    return new BRegister(parsedNum, parsedRegType, name);

                } else if(Enum.TryParse<SpecialAddress>(areaGroup, out var parsedSpecial) && isBool) {

                    return new BRegister(parsedSpecial, parsedRegType, name);

                }
            }

            throw new NotSupportedException($"Register with value {regString} is not supported");

        }

        public static NRegister<T> FromString<T> (string regString, string name = null) {

            var match = Regex.Match(regString, @"(DT|DDT)([0-9_.-]{1,5})");

            if (match != null && match.Success) {

                var typeGroup = match.Groups[1].Value;
                var areaGroup = match.Groups[2].Value;

                bool isTypeDoubleSize = false;
                bool isSupportedNumericFormat = false;

                if(typeGroup == "")

                switch (typeGroup) {
                    case "DT":
                    isSupportedNumericFormat = true;
                    break;
                    case "DDT":
                    isTypeDoubleSize = true;
                    isSupportedNumericFormat = true;
                    break;
                }

                if(typeof(T).IsDoubleNumericRegisterType() != isTypeDoubleSize) {
                    throw new NotSupportedException($"Input register type was {typeGroup}, the cast type was not of the same size");
                }

                if (int.TryParse(areaGroup, out var parsedNum) && typeof(T).IsNumericSupportedType() && isSupportedNumericFormat ) {

                    return new NRegister<T>(parsedNum, name);

                } 

            }

            throw new NotSupportedException($"Register with value {regString} is not supported");

        }

        public static SRegister FromString (string regString, int reserved, string name = null) {

            var match = Regex.Match(regString, @"(DT)([0-9_.-]{1,5})");

            if (match != null && match.Success) {

              
            }

            throw new NotSupportedException($"Register with value {regString} is not supported");

        }

        internal string GetCombinedName () {

            return $"{(CollectionType != null ? $"{CollectionType.Name}." : "")}{Name ?? "Unnamed"}";

        }

        internal string GetContainerName () {

            return $"{(CollectionType != null ? $"{CollectionType.Name}" : "")}";

        }

        internal string GetRegisterPLCName () {

            if (this is BRegister bReg && bReg.SpecialAddress != SpecialAddress.None) {
                return $"{GetRegisterString()}{bReg.SpecialAddress}";
            }

            return $"{GetRegisterString()}{MemoryAdress}";

        }

    }

}
