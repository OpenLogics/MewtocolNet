using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MewtocolNet.Responses;

namespace MewtocolNet {
    public partial class MewtocolInterface {

        public event Action<Register> RegisterChanged;

        internal CancellationTokenSource cTokenAutoUpdater;
        protected internal bool isWriting = false;
       
        
        public bool ContinousReaderRunning { get; set; }
        public List<Register> Registers { get; set; } = new List<Register>();
        public List<Contact> Contacts { get; set; } = new List<Contact>();

        /// <summary>
        /// Trys to connect to the PLC by the IP given in the constructor
        /// </summary>
        /// <param name="OnConnected">Gets called when a connection with a PLC was established</param>
        /// <param name="OnFailed">Gets called when an error or timeout during connection occurs</param>
        /// <returns></returns>
        public async Task<MewtocolInterface> ConnectAsync (Action<PLCInfo> OnConnected = null, Action OnFailed = null) {

            var plcinf = await GetPLCInfoAsync();

            if(plcinf is not null) {

                if(OnConnected != null) OnConnected(plcinf);

            } else {

                if(OnFailed != null) OnFailed();

            }

            return this;

        }

        #region Register Adding

        /// <summary>
        /// Adds a PLC memory register to the watchlist <para/>
        /// The registers can be read back by attaching <see cref="MewtocolInterfaceExtensions.AttachContinousReader(Task{MewtocolInterface}, int)"/>
        /// <para/>
        /// to the end of a <see cref="MewtocolInterface.ConnectAsync(Action{PLCInfo}, Action)"/> method
        /// </summary>
        /// <typeparam name="T">
        /// The type of the register translated from C# to IEC 61131-3 types
        /// <para>C# ------ IEC</para>
        /// <para>short => INT/WORD</para>
        /// <para>ushort => UINT</para>
        /// <para>int => DOUBLE</para>
        /// <para>uint => UDOUBLE</para>
        /// <para>float => REAL</para>
        /// <para>string => STRING</para>
        /// </typeparam>
        /// <param name="_address">The address of the register in the PLCs memory</param>
        /// <param name="_length">The length of the string (Can be ignored for other types)</param>
        public void AddRegister<T> (int _address, int _length = 1) {

            Type regType = typeof(T);

            if (regType == typeof(short)) {
                Registers.Add(new NRegister<short>(_address));
            } else if (regType == typeof(ushort)) {
                Registers.Add(new NRegister<ushort>(_address));
            } else if (regType == typeof(int)) {
                Registers.Add(new NRegister<int>(_address));
            } else if (regType == typeof(uint)) {
                Registers.Add(new NRegister<uint>(_address));
            } else if (regType == typeof(float)) {
                Registers.Add(new NRegister<float>(_address));
            } else if (regType == typeof(string)) {
                Registers.Add(new SRegister(_address, _length));
            } else {
                throw new NotSupportedException($"The type {regType} is not allowed for Registers \n" +
                                                $"Allowed are: short, ushort, int, uint, float and string");
            }

        }

        /// <summary>
        /// Adds a PLC memory register to the watchlist <para/>
        /// The registers can be read back by attaching <see cref="MewtocolInterfaceExtensions.AttachContinousReader(Task{MewtocolInterface}, int)"/>
        /// <para/>
        /// to the end of a <see cref="MewtocolInterface.ConnectAsync(Action{PLCInfo}, Action)"/> method
        /// </summary>
        /// <typeparam name="T">
        /// The type of the register translated from C# to IEC 61131-3 types
        /// <para>C# ------ IEC</para>
        /// <para>short => INT/WORD</para>
        /// <para>ushort => UINT</para>
        /// <para>int => DOUBLE</para>
        /// <para>uint => UDOUBLE</para>
        /// <para>float => REAL</para>
        /// <para>string => STRING</para>
        /// </typeparam>
        /// <param name="_name">A naming definition for QOL, doesn't effect PLC and is optional</param>
        /// <param name="_address">The address of the register in the PLCs memory</param>
        /// <param name="_length">The length of the string (Can be ignored for other types)</param>
        public void AddRegister<T>(string _name, int _address, int _length = 1) {

            Type regType = typeof(T);

            if (regType == typeof(short)) {
                Registers.Add(new NRegister<short>(_address, _name));
            } else if (regType == typeof(ushort)) {
                Registers.Add(new NRegister<ushort>(_address, _name));
            } else if (regType == typeof(int)) {
                Registers.Add(new NRegister<int>(_address, _name));
            } else if (regType == typeof(uint)) {
                Registers.Add(new NRegister<uint>(_address, _name));
            } else if (regType == typeof(float)) {
                Registers.Add(new NRegister<float>(_address, _name));
            } else if (regType == typeof(string)) {
                Registers.Add(new SRegister(_address, _length, _name));
            } else {
                throw new NotSupportedException($"The type {regType} is not allowed for Registers \n" +
                                                $"Allowed are: short, ushort, int, uint, float and string");
            }

        }

        #endregion

        internal void InvokeRegisterChanged (Register reg) {

            RegisterChanged?.Invoke (reg);      

        }

    }
}
