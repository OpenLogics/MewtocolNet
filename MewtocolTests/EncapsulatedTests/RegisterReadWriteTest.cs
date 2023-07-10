using MewtocolNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolTests.EncapsulatedTests;

internal class RegisterReadWriteTest {

    public IRegister TargetRegister { get; set; }   

    public object IntialValue { get; set; }

    public object IntermediateValue { get; set; } 

    public object AfterWriteValue { get; set; }

    public string RegisterPlcAddressName { get; set; }

}
