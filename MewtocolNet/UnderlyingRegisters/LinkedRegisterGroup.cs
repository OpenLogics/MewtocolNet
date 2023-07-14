using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.UnderlyingRegisters {

    internal class LinkedRegisterGroup {

        internal uint AddressStart;

        internal uint AddressEnd;     

        internal List<BaseRegister> Linked = new List<BaseRegister>();  

    }

}
