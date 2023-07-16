using MewtocolNet.Registers;
using System.Collections.Generic;

namespace MewtocolNet.UnderlyingRegisters {

    internal class LinkedRegisterGroup {

        internal uint AddressStart;

        internal uint AddressEnd;

        internal List<Register> Linked = new List<Register>();

    }

}
