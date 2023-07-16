using MewtocolNet.Registers;
using System;

namespace MewtocolNet.Exceptions {

    [Serializable]
    public class MewtocolException : Exception {

        public MewtocolException() { }

        public MewtocolException(string message) : base(message) { }

        public MewtocolException(string message, Exception inner) : base(message, inner) { }

        protected MewtocolException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        internal static MewtocolException NotConnectedSend() {

            return new MewtocolException($"Can not send a message to the PLC if it isn't connected");

        }

        internal static MewtocolException DupeRegister(Register register) {

            return new MewtocolException($"The mewtocol interface already contains this register: {register.GetMewName()}");

        }

        internal static MewtocolException DupeNameRegister(Register register) {

            return new MewtocolException($"The mewtocol interface registers already contains a register with the name: {register.GetMewName()}");

        }

        internal static MewtocolException OverlappingRegister(Register registerA, Register registerB) {

            return new MewtocolException($"The register: {registerA.GetRegisterWordRangeString()} " +
                        $"has overlapping addresses with: {registerB.GetRegisterWordRangeString()}");

        }

    }

}
