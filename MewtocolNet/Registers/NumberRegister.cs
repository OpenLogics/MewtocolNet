﻿using MewtocolNet.Exceptions;
using MewtocolNet.UnderlyingRegisters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static MewtocolNet.RegisterBuilding.RBuild;

namespace MewtocolNet.Registers {

    /// <summary>
    /// Defines a register containing a number
    /// </summary>
    /// <typeparam name="T">The type of the numeric value</typeparam>
    public class NumberRegister<T> : BaseRegister {

        /// <summary>
        /// Defines a register containing a number
        /// </summary>
        /// <param name="_address">Memory start adress max 99999</param>
        /// <param name="_name">Name of the register</param>
        public NumberRegister (uint _address, string _name = null) {

            memoryAddress = _address;
            name = _name;

            Type numType = typeof(T);
            uint areaLen = 0;

            if (typeof(T).IsEnum) {

                //for enums

                var underlyingType = typeof(T).GetEnumUnderlyingType(); //the numeric type
                areaLen = (uint)(Marshal.SizeOf(underlyingType) / 2) - 1;

                if (areaLen == 0) RegisterType = RegisterType.DT;
                if (areaLen == 1) RegisterType = RegisterType.DDT;
                if (areaLen >= 2) RegisterType = RegisterType.DT_BYTE_RANGE;

                lastValue = null;
                Console.WriteLine();

            } else {

                //for all others known pre-defined numeric structs

                var allowedTypes = PlcValueParser.GetAllowDotnetTypes();
                if (!allowedTypes.Contains(numType))
                    throw new NotSupportedException($"The type {numType} is not allowed for Number Registers");

                areaLen = (uint)(Marshal.SizeOf(numType) / 2) - 1;
                RegisterType = areaLen >= 1 ? RegisterType.DDT : RegisterType.DT;

                lastValue = null;

            }

            CheckAddressOverflow(memoryAddress, areaLen);

        }

        /// <inheritdoc/>
        public override void SetValueFromPLC(object val) {

            if (lastValue?.ToString() != val?.ToString()) {

                if (val != null) lastValue = (T)val;
                else lastValue = null;

                TriggerNotifyChange();
                attachedInterface.InvokeRegisterChanged(this);

            }

        }

        /// <inheritdoc/>
        public override string BuildMewtocolQuery() {

            StringBuilder asciistring = new StringBuilder("D");
            asciistring.Append(MemoryAddress.ToString().PadLeft(5, '0'));

            int offsetAddress = 0;
            if(RegisterType == RegisterType.DDT)
                offsetAddress = 1;

            asciistring.Append((MemoryAddress + offsetAddress).ToString().PadLeft(5, '0'));
            return asciistring.ToString();

        }

        /// <inheritdoc/>
        public override string GetAsPLC() {

            if (typeof(T) == typeof(TimeSpan)) return ((TimeSpan)Value).ToPlcTime();

            return Value.ToString();

        }

        /// <inheritdoc/>
        public override string GetValueString() {

            if(Value != null && typeof(T) == typeof(TimeSpan)) {

                return $"{Value} [{((TimeSpan)Value).ToPlcTime()}]";

            } 

            if (Value != null && typeof(T).IsEnum) {

                var underlying = Enum.GetUnderlyingType(typeof(T));
                object val = Convert.ChangeType(Value, underlying);

                return $"{Value} [{val}]";

            }

            return Value?.ToString() ?? "null";

        }

        /// <inheritdoc/>
        public override uint GetRegisterAddressLen() => (uint)(RegisterType == RegisterType.DT ? 1 : 2);

        /// <inheritdoc/>
        public override async Task<object> ReadAsync() {

            if (!attachedInterface.IsConnected) return null;

            var res = await underlyingMemory.ReadRegisterAsync(this);
            if (!res) return null;

            var bytes = underlyingMemory.GetUnderlyingBytes(this);
            
            return SetValueFromBytes(bytes);

        }

        internal override object SetValueFromBytes(byte[] bytes) {

            AddSuccessRead();

            var parsed = PlcValueParser.Parse<T>(this, bytes);
            SetValueFromPLC(parsed);
            return parsed;

        }

        /// <inheritdoc/>
        public override async Task<bool> WriteAsync(object data) {

            if (!attachedInterface.IsConnected) return false;

            var encoded = PlcValueParser.Encode(this, (T)data);
            var res = await underlyingMemory.WriteRegisterAsync(this, encoded);

            if (res) {
                AddSuccessWrite();
                SetValueFromPLC(data);
            }

            return res;

        }

        internal override async Task<bool> WriteToAnonymousAsync (object value) {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            var encoded = PlcValueParser.Encode(this, (T)value);
            return await attachedInterface.WriteByteRange((int)MemoryAddress, encoded);

        }

        internal override async Task<object> ReadFromAnonymousAsync () {

            if (!attachedInterface.IsConnected)
                throw MewtocolException.NotConnectedSend();

            var res = await attachedInterface.ReadByteRangeNonBlocking((int)MemoryAddress, (int)GetRegisterAddressLen() * 2);
            if (res == null) return null;

            return PlcValueParser.Parse<T>(this, res);

        }

    }

}
