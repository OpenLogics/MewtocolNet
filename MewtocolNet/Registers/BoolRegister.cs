using MewtocolNet.Exceptions;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.UnderlyingRegisters;
using System;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a boolean
    /// </summary>
    public class BoolRegister : BaseRegister {

        internal byte specialAddress;
        /// <summary>
        /// The registers memory adress if not a special register
        /// </summary>
        public byte SpecialAddress => specialAddress;

        [Obsolete("Creating registers directly is not supported use IPlc.Register instead")]
        public BoolRegister() => 
        throw new NotSupportedException("Direct register instancing is not supported, use the builder pattern");   
        
        internal BoolRegister(IOType _io, byte _spAddress = 0x0, uint _areaAdress = 0, string _name = null) {

            lastValue = null;

            memoryAddress = _areaAdress;
            specialAddress = _spAddress;
            name = _name;

            RegisterType = (RegisterType)(int)_io;

            CheckAddressOverflow(memoryAddress, 0);

        }

        protected override void CheckAddressOverflow(uint addressStart, uint addressLen) {

            if ((int)RegisterType == (int)IOType.R && addressStart >= 512)
                throw new NotSupportedException("R area addresses cant be greater than 511");

            if (((int)RegisterType == (int)IOType.X || (int)RegisterType == (int)IOType.Y) && addressStart >= 110)
                throw new NotSupportedException("XY area addresses cant be greater than 110");

            if (specialAddress > 0xF)
                throw new NotSupportedException("Special address cant be greater than 15 or 0xF");

            base.CheckAddressOverflow(addressStart, addressLen);

        }

        #region Read / Write

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            return null;

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            return false;

        }

        internal override async Task<bool> WriteToAnonymousAsync (object value) {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            var station = attachedInterface.GetStationNumber();
            string reqStr = $"%{station}#WCS{BuildMewtocolQuery()}{((bool)value ? "1" : "0")}";
            var res = await attachedInterface.SendCommandAsync(reqStr);

            return res.Success;

        }

        internal override async Task<object> ReadFromAnonymousAsync() {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            var station = attachedInterface.GetStationNumber();
            string requeststring = $"%{station}#RCS{BuildMewtocolQuery()}";
            var result = await attachedInterface.SendCommandAsync(requeststring);
            if (!result.Success) return null;

            return result.Response.ParseRCSingleBit();

        }

        #endregion

        /// <inheritdoc/>
        public override byte? GetSpecialAddress() => SpecialAddress;

        /// <inheritdoc/>
        public override string BuildMewtocolQuery() {

            //(R|X|Y)(area add [3] + special add [1])
            StringBuilder asciistring = new StringBuilder();

            string prefix = RegisterType.ToString();
            string mem = MemoryAddress.ToString();
            string sp = SpecialAddress.ToString("X1");

            asciistring.Append(prefix);
            asciistring.Append(mem.PadLeft(3, '0'));
            asciistring.Append(sp);

            return asciistring.ToString();

        }

        /// <inheritdoc/>
        public override string GetMewName() {

            var spAdressEnd = SpecialAddress.ToString("X1");

            if (MemoryAddress == 0) {

                return $"{GetRegisterString()}{spAdressEnd}";

            }

            if (MemoryAddress > 0 && SpecialAddress != 0) {

                return $"{GetRegisterString()}{MemoryAddress}{spAdressEnd}";

            }

            return $"{GetRegisterString()}{MemoryAddress}";

        }

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen () => 1;

    }

}
