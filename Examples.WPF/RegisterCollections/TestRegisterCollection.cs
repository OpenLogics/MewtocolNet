using MewtocolNet;
using MewtocolNet.RegisterAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.WPF.RegisterCollections;

public class TestRegisterCollection : RegisterCollection {

    [Register("R11A")]
    public bool? TestR11A { get; set; }

    [Register("R11A")]
    public bool TestR11A_Duplicate_NonNullable { get; set; }

    [Register("R16B")]
    public bool TestR16B { get; set; }

    [BitRegister("DT1000", 0), PollLevel(3)]
    public bool? TestDT100_Word_Duplicate_SingleBit { get; set; }

    [Register("DT1000")]
    public Word TestDT100_Word_Duplicate { get; set; }

    [BitRegister("DDT1010", 1)]
    public bool? TestDDT1010_DWord_Duplicate_SingleBit { get; set; }

}
