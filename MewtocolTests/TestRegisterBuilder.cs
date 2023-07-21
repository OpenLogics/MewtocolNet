using MewtocolNet;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.RegisterBuilding.BuilderPatterns;
using MewtocolNet.Registers;
using MewtocolTests.EncapsulatedTests;
using System.Collections;
using Xunit;
using Xunit.Abstractions;
using static System.Net.Mime.MediaTypeNames;

namespace MewtocolTests;

public class TestRegisterBuilder {

    private readonly ITestOutputHelper output;

    public TestRegisterBuilder(ITestOutputHelper output) {
        this.output = output;
    }

    [Fact(DisplayName = "Parsing as Bool Register List (Phyiscal Outputs)")]
    public void TestParsingBRegisterY() {

        var tests = new Dictionary<string, IRegister>() {

            {"Y0",  new BoolRegister(SingleBitPrefix.Y)},
            {"Y1",  new BoolRegister(SingleBitPrefix.Y, 0x1)},
            {"Y2",  new BoolRegister(SingleBitPrefix.Y, 0x2)},
            {"Y3",  new BoolRegister(SingleBitPrefix.Y, 0x3)},
            {"Y4",  new BoolRegister(SingleBitPrefix.Y, 0x4)},
            {"Y5",  new BoolRegister(SingleBitPrefix.Y, 0x5)},
            {"Y6",  new BoolRegister(SingleBitPrefix.Y, 0x6)},
            {"Y7",  new BoolRegister(SingleBitPrefix.Y, 0x7)},
            {"Y8",  new BoolRegister(SingleBitPrefix.Y, 0x8)},
            {"Y9",  new BoolRegister(SingleBitPrefix.Y, 0x9)},

            {"YA",  new BoolRegister(SingleBitPrefix.Y, 0xA)},
            {"YB",  new BoolRegister(SingleBitPrefix.Y, 0xB)},
            {"YC",  new BoolRegister(SingleBitPrefix.Y, 0xC)},
            {"YD",  new BoolRegister(SingleBitPrefix.Y, 0xD)},
            {"YE",  new BoolRegister(SingleBitPrefix.Y, 0xE)},
            {"YF",  new BoolRegister(SingleBitPrefix.Y, 0xF)},

            {"Y1A",  new BoolRegister(SingleBitPrefix.Y, 0xA, 1)},
            {"Y10B",  new BoolRegister(SingleBitPrefix.Y, 0xB, 10)},
            {"Y109C",  new BoolRegister(SingleBitPrefix.Y, 0xC, 109)},

        };

        TestBoolDict(tests);

    }

    [Fact(DisplayName = "Parsing as Bool Register List (Phyiscal Inputs)")]
    public void TestParsingBRegisterX() {

        var tests = new Dictionary<string, IRegister>() {

            {"X0",  new BoolRegister(SingleBitPrefix.X)},
            {"X1",  new BoolRegister(SingleBitPrefix.X, 0x1)},
            {"X2",  new BoolRegister(SingleBitPrefix.X, 0x2)},
            {"X3",  new BoolRegister(SingleBitPrefix.X, 0x3)},
            {"X4",  new BoolRegister(SingleBitPrefix.X, 0x4)},
            {"X5",  new BoolRegister(SingleBitPrefix.X, 0x5)},
            {"X6",  new BoolRegister(SingleBitPrefix.X, 0x6)},
            {"X7",  new BoolRegister(SingleBitPrefix.X, 0x7)},
            {"X8",  new BoolRegister(SingleBitPrefix.X, 0x8)},
            {"X9",  new BoolRegister(SingleBitPrefix.X, 0x9)},

            {"XA",  new BoolRegister(SingleBitPrefix.X, 0xA)},
            {"XB",  new BoolRegister(SingleBitPrefix.X, 0xB)},
            {"XC",  new BoolRegister(SingleBitPrefix.X, 0xC)},
            {"XD",  new BoolRegister(SingleBitPrefix.X, 0xD)},
            {"XE",  new BoolRegister(SingleBitPrefix.X, 0xE)},
            {"XF",  new BoolRegister(SingleBitPrefix.X, 0xF)},

            {"X1A",  new BoolRegister(SingleBitPrefix.X, 0xA, 1)},
            {"X10B",  new BoolRegister(SingleBitPrefix.X, 0xB, 10)},
            {"X109C",  new BoolRegister(SingleBitPrefix.X, 0xC, 109)},

        };

        TestBoolDict(tests);

    }

    [Fact(DisplayName = "Parsing as Bool Register List (Internal Relay)")]
    public void TestParsingBRegisterR() {

        var tests = new Dictionary<string, IRegister>() {

            {"R0",  new BoolRegister(SingleBitPrefix.R)},
            {"R1",  new BoolRegister(SingleBitPrefix.R, 0x1)},
            {"R2",  new BoolRegister(SingleBitPrefix.R, 0x2)},
            {"R3",  new BoolRegister(SingleBitPrefix.R, 0x3)},
            {"R4",  new BoolRegister(SingleBitPrefix.R, 0x4)},
            {"R5",  new BoolRegister(SingleBitPrefix.R, 0x5)},
            {"R6",  new BoolRegister(SingleBitPrefix.R, 0x6)},
            {"R7",  new BoolRegister(SingleBitPrefix.R, 0x7)},
            {"R8",  new BoolRegister(SingleBitPrefix.R, 0x8)},
            {"R9",  new BoolRegister(SingleBitPrefix.R, 0x9)},

            {"RA",  new BoolRegister(SingleBitPrefix.R, 0xA)},
            {"RB",  new BoolRegister(SingleBitPrefix.R, 0xB)},
            {"RC",  new BoolRegister(SingleBitPrefix.R, 0xC)},
            {"RD",  new BoolRegister(SingleBitPrefix.R, 0xD)},
            {"RE",  new BoolRegister(SingleBitPrefix.R, 0xE)},
            {"RF",  new BoolRegister(SingleBitPrefix.R, 0xF)},

            {"R1A",  new BoolRegister(SingleBitPrefix.R, 0xA, 1)},
            {"R10B",  new BoolRegister(SingleBitPrefix.R, 0xB, 10)},
            {"R109C",  new BoolRegister(SingleBitPrefix.R, 0xC, 109)},
            {"R1000",  new BoolRegister(SingleBitPrefix.R, 0x0, 100)},
            {"R511",  new BoolRegister(SingleBitPrefix.R, 0x0, 511)},
            {"R511A",  new BoolRegister(SingleBitPrefix.R, 0xA, 511)},

        };

        TestBoolDict(tests);

    }

    private void TestBoolDict (Dictionary<string, IRegister> dict) {

        foreach (var item in dict) {


        }

    }

}
