using MewtocolNet;
using MewtocolNet.Mewtocol;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests;

public class TestRegisterParsing {

    private readonly ITestOutputHelper output;

    public TestRegisterParsing (ITestOutputHelper output) {
        this.output = output;
    }

    [Fact(DisplayName = "Parsing as BRegister (Phyiscal Outputs)")]
    public void TestParsingBRegisterY () {

        var tests = new Dictionary<string, IRegister>() {

            {"Y0",  new BRegister(IOType.Y)},
            {"Y1",  new BRegister(IOType.Y, 0x1)},
            {"Y2",  new BRegister(IOType.Y, 0x2)},
            {"Y3",  new BRegister(IOType.Y, 0x3)},
            {"Y4",  new BRegister(IOType.Y, 0x4)},
            {"Y5",  new BRegister(IOType.Y, 0x5)},
            {"Y6",  new BRegister(IOType.Y, 0x6)},
            {"Y7",  new BRegister(IOType.Y, 0x7)},
            {"Y8",  new BRegister(IOType.Y, 0x8)},
            {"Y9",  new BRegister(IOType.Y, 0x9)},

            {"YA",  new BRegister(IOType.Y, 0xA)},
            {"YB",  new BRegister(IOType.Y, 0xB)},
            {"YC",  new BRegister(IOType.Y, 0xC)},
            {"YD",  new BRegister(IOType.Y, 0xD)},
            {"YE",  new BRegister(IOType.Y, 0xE)},
            {"YF",  new BRegister(IOType.Y, 0xF)},

            {"Y1A",  new BRegister(IOType.Y, 0xA, 1)},
            {"Y10B",  new BRegister(IOType.Y, 0xB, 10)},
            {"Y109C",  new BRegister(IOType.Y, 0xC, 109)},

        };

    }

    [Fact(DisplayName = "Parsing as BRegister (Phyiscal Inputs)")]
    public void TestParsingBRegisterX () {

        var tests = new Dictionary<string, IRegister>() {

            {"X0",  new BRegister(IOType.X)},
            {"X1",  new BRegister(IOType.X, 0x1)},
            {"X2",  new BRegister(IOType.X, 0x2)},
            {"X3",  new BRegister(IOType.X, 0x3)},
            {"X4",  new BRegister(IOType.X, 0x4)},
            {"X5",  new BRegister(IOType.X, 0x5)},
            {"X6",  new BRegister(IOType.X, 0x6)},
            {"X7",  new BRegister(IOType.X, 0x7)},
            {"X8",  new BRegister(IOType.X, 0x8)},
            {"X9",  new BRegister(IOType.X, 0x9)},

            {"XA",  new BRegister(IOType.X, 0xA)},
            {"XB",  new BRegister(IOType.X, 0xB)},
            {"XC",  new BRegister(IOType.X, 0xC)},
            {"XD",  new BRegister(IOType.X, 0xD)},
            {"XE",  new BRegister(IOType.X, 0xE)},
            {"XF",  new BRegister(IOType.X, 0xF)},

            {"X1A",  new BRegister(IOType.X, 0xA, 1)},
            {"X10B",  new BRegister(IOType.X, 0xB, 10)},
            {"X109C",  new BRegister(IOType.X, 0xC, 109)},

        };

    }

}
