using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.Exceptions {

    [Serializable]
    public class MewtocolException : Exception {

        public MewtocolException() { }

        public MewtocolException(string message) : base(message) { }

        public MewtocolException(string message, Exception inner) : base(message, inner) { }

        protected MewtocolException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        internal static MewtocolException DupeRegister (IRegisterInternal register) {

            return new MewtocolException($"The mewtocol interface already contains this register: {register.GetRegisterPLCName()}");

        }

        internal static MewtocolException DupeNameRegister (IRegisterInternal register) {

            return new MewtocolException($"The mewtocol interface registers already contains a register with the name: {register.GetRegisterPLCName()}");

        }

    }

}
