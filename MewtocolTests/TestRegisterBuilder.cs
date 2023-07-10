using MewtocolNet;
using MewtocolNet.RegisterBuilding;
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

            {"Y0",  new BoolRegister(IOType.Y)},
            {"Y1",  new BoolRegister(IOType.Y, 0x1)},
            {"Y2",  new BoolRegister(IOType.Y, 0x2)},
            {"Y3",  new BoolRegister(IOType.Y, 0x3)},
            {"Y4",  new BoolRegister(IOType.Y, 0x4)},
            {"Y5",  new BoolRegister(IOType.Y, 0x5)},
            {"Y6",  new BoolRegister(IOType.Y, 0x6)},
            {"Y7",  new BoolRegister(IOType.Y, 0x7)},
            {"Y8",  new BoolRegister(IOType.Y, 0x8)},
            {"Y9",  new BoolRegister(IOType.Y, 0x9)},

            {"YA",  new BoolRegister(IOType.Y, 0xA)},
            {"YB",  new BoolRegister(IOType.Y, 0xB)},
            {"YC",  new BoolRegister(IOType.Y, 0xC)},
            {"YD",  new BoolRegister(IOType.Y, 0xD)},
            {"YE",  new BoolRegister(IOType.Y, 0xE)},
            {"YF",  new BoolRegister(IOType.Y, 0xF)},

            {"Y1A",  new BoolRegister(IOType.Y, 0xA, 1)},
            {"Y10B",  new BoolRegister(IOType.Y, 0xB, 10)},
            {"Y109C",  new BoolRegister(IOType.Y, 0xC, 109)},

        };

        TestBoolDict(tests);

    }

    [Fact(DisplayName = "Parsing as Bool Register List (Phyiscal Inputs)")]
    public void TestParsingBRegisterX() {

        var tests = new Dictionary<string, IRegister>() {

            {"X0",  new BoolRegister(IOType.X)},
            {"X1",  new BoolRegister(IOType.X, 0x1)},
            {"X2",  new BoolRegister(IOType.X, 0x2)},
            {"X3",  new BoolRegister(IOType.X, 0x3)},
            {"X4",  new BoolRegister(IOType.X, 0x4)},
            {"X5",  new BoolRegister(IOType.X, 0x5)},
            {"X6",  new BoolRegister(IOType.X, 0x6)},
            {"X7",  new BoolRegister(IOType.X, 0x7)},
            {"X8",  new BoolRegister(IOType.X, 0x8)},
            {"X9",  new BoolRegister(IOType.X, 0x9)},

            {"XA",  new BoolRegister(IOType.X, 0xA)},
            {"XB",  new BoolRegister(IOType.X, 0xB)},
            {"XC",  new BoolRegister(IOType.X, 0xC)},
            {"XD",  new BoolRegister(IOType.X, 0xD)},
            {"XE",  new BoolRegister(IOType.X, 0xE)},
            {"XF",  new BoolRegister(IOType.X, 0xF)},

            {"X1A",  new BoolRegister(IOType.X, 0xA, 1)},
            {"X10B",  new BoolRegister(IOType.X, 0xB, 10)},
            {"X109C",  new BoolRegister(IOType.X, 0xC, 109)},

        };

        TestBoolDict(tests);

    }

    [Fact(DisplayName = "Parsing as Bool Register List (Internal Relay)")]
    public void TestParsingBRegisterR() {

        var tests = new Dictionary<string, IRegister>() {

            {"R0",  new BoolRegister(IOType.R)},
            {"R1",  new BoolRegister(IOType.R, 0x1)},
            {"R2",  new BoolRegister(IOType.R, 0x2)},
            {"R3",  new BoolRegister(IOType.R, 0x3)},
            {"R4",  new BoolRegister(IOType.R, 0x4)},
            {"R5",  new BoolRegister(IOType.R, 0x5)},
            {"R6",  new BoolRegister(IOType.R, 0x6)},
            {"R7",  new BoolRegister(IOType.R, 0x7)},
            {"R8",  new BoolRegister(IOType.R, 0x8)},
            {"R9",  new BoolRegister(IOType.R, 0x9)},

            {"RA",  new BoolRegister(IOType.R, 0xA)},
            {"RB",  new BoolRegister(IOType.R, 0xB)},
            {"RC",  new BoolRegister(IOType.R, 0xC)},
            {"RD",  new BoolRegister(IOType.R, 0xD)},
            {"RE",  new BoolRegister(IOType.R, 0xE)},
            {"RF",  new BoolRegister(IOType.R, 0xF)},

            {"R1A",  new BoolRegister(IOType.R, 0xA, 1)},
            {"R10B",  new BoolRegister(IOType.R, 0xB, 10)},
            {"R109C",  new BoolRegister(IOType.R, 0xC, 109)},
            {"R1000",  new BoolRegister(IOType.R, 0x0, 100)},
            {"R511",  new BoolRegister(IOType.R, 0x0, 511)},
            {"R511A",  new BoolRegister(IOType.R, 0xA, 511)},

        };

        TestBoolDict(tests);

    }

    private void TestBoolDict (Dictionary<string, IRegister> dict) {

        foreach (var item in dict) {

            output.WriteLine($"Expected: {item.Key}");

            var built = RegBuilder.Factory.FromPlcRegName(item.Key).AsPlcType(PlcVarType.BOOL).Build();

            output.WriteLine($"{(built?.ToString(true) ?? "null")}\n");
            Assert.Equivalent(item.Value, built);

        }

    }

    [Fact(DisplayName = "Parsing as Bool Register (Casted)")]
    public void TestRegisterBuildingBoolCasted () {

        var expect = new BoolRegister(IOType.R, 0x1, 0);
        var expect2 = new BoolRegister(IOType.Y, 0xA, 103);

        Assert.Equivalent(expect, RegBuilder.Factory.FromPlcRegName("R1").AsPlcType(PlcVarType.BOOL).Build());
        Assert.Equivalent(expect, RegBuilder.Factory.FromPlcRegName("R1").AsType<bool>().Build());

        Assert.Equivalent(expect2, RegBuilder.Factory.FromPlcRegName("Y103A").AsPlcType(PlcVarType.BOOL).Build());
        Assert.Equivalent(expect2, RegBuilder.Factory.FromPlcRegName("Y103A").AsType<bool>().Build());

    }

    [Fact(DisplayName = "Parsing as Bool Register (Auto)")]
    public void TestRegisterBuildingBoolAuto () {

        var expect = new BoolRegister(IOType.R, 0x1, 0);
        var expect2 = new BoolRegister(IOType.Y, 0xA, 103);

        Assert.Equivalent(expect, RegBuilder.Factory.FromPlcRegName("R1").Build());
        Assert.Equivalent(expect, RegBuilder.Factory.FromPlcRegName("R1").Build());

        Assert.Equivalent(expect2, RegBuilder.Factory.FromPlcRegName("Y103A").Build());
        Assert.Equivalent(expect2, RegBuilder.Factory.FromPlcRegName("Y103A").Build());

    }

    [Fact(DisplayName = "Parsing as Number Register (Casted)")]
    public void TestRegisterBuildingNumericCasted () {

        var expect = new NumberRegister<short>(303);
        var expect2 = new NumberRegister<int>(10002);
        var expect3 = new NumberRegister<float>(404);
        var expect4 = new NumberRegister<TimeSpan>(400);
        var expect5 = new NumberRegister<CurrentState>(203);
        var expect6 = new NumberRegister<CurrentState32>(204);

        Assert.Equivalent(expect, RegBuilder.Factory.FromPlcRegName("DT303").AsPlcType(PlcVarType.INT).Build());
        Assert.Equivalent(expect, RegBuilder.Factory.FromPlcRegName("DT303").AsType<short>().Build());

        Assert.Equivalent(expect2, RegBuilder.Factory.FromPlcRegName("DDT10002").AsPlcType(PlcVarType.DINT).Build());
        Assert.Equivalent(expect2, RegBuilder.Factory.FromPlcRegName("DDT10002").AsType<int>().Build());

        Assert.Equivalent(expect3, RegBuilder.Factory.FromPlcRegName("DDT404").AsPlcType(PlcVarType.REAL).Build());
        Assert.Equivalent(expect3, RegBuilder.Factory.FromPlcRegName("DDT404").AsType<float>().Build());

        Assert.Equivalent(expect4, RegBuilder.Factory.FromPlcRegName("DDT400").AsPlcType(PlcVarType.TIME).Build());
        Assert.Equivalent(expect4, RegBuilder.Factory.FromPlcRegName("DDT400").AsType<TimeSpan>().Build());

        Assert.Equivalent(expect5, RegBuilder.Factory.FromPlcRegName("DT203").AsType<CurrentState>().Build());
        Assert.Equivalent(expect6, RegBuilder.Factory.FromPlcRegName("DT204").AsType<CurrentState32>().Build());

    }

    [Fact(DisplayName = "Parsing as Number Register (Auto)")]
    public void TestRegisterBuildingNumericAuto () {

        var expect = new NumberRegister<short>(201);
        var expect2 = new NumberRegister<int>(10002);

        Assert.Equivalent(expect, RegBuilder.Factory.FromPlcRegName("DT201").Build());
        Assert.Equivalent(expect2, RegBuilder.Factory.FromPlcRegName("DDT10002").Build());

    }

    [Fact(DisplayName = "Parsing as Bytes Register (Casted)")]
    public void TestRegisterBuildingByteRangeCasted () {

        var expect = new BytesRegister(305, (uint)35);

        Assert.Equal((uint)18, expect.AddressLength);
        Assert.Equivalent(expect, RegBuilder.Factory.FromPlcRegName("DT305").AsBytes(35).Build());

    }

    [Fact(DisplayName = "Parsing as Bytes Register (Auto)")]
    public void TestRegisterBuildingByteRangeAuto () {

        var expect = new BytesRegister(300, (uint)20 * 2);
        var actual = (BytesRegister)RegBuilder.Factory.FromPlcRegName("DT300-DT319").Build();

        Assert.Equal((uint)20, expect.AddressLength);
        Assert.Equivalent(expect, actual);

    }

    [Fact(DisplayName = "Parsing as Bit Array")]
    public void TestRegisterBuildingBitArray () {

        var expect1 = new BytesRegister(311, (ushort)5);
        var expect2 = new BytesRegister(312, (ushort)16);
        var expect3 = new BytesRegister(313, (ushort)32);

        var actual1 = (BytesRegister)RegBuilder.Factory.FromPlcRegName("DT311").AsBits(5).Build();
        var actual2 = (BytesRegister)RegBuilder.Factory.FromPlcRegName("DT312").AsBits(16).Build();
        var actual3 = (BytesRegister)RegBuilder.Factory.FromPlcRegName("DT313").AsBits(32).Build();

        Assert.Equivalent(expect1, actual1);
        Assert.Equivalent(expect2, actual2);
        Assert.Equivalent(expect3, actual3);

        Assert.Equal((uint)1, actual1.AddressLength);
        Assert.Equal((uint)1, actual2.AddressLength);
        Assert.Equal((uint)2, actual3.AddressLength);

    }

    [Fact(DisplayName = "Parsing as String Register")]
    public void TestRegisterBuildingString () {

        var expect1 = new StringRegister(314);

        var actual1 = (StringRegister)RegBuilder.Factory.FromPlcRegName("DT314").AsType<string>().Build();

        Assert.Equivalent(expect1, actual1);

        Assert.Equal((uint)0, actual1.WordsSize);

    }


}
