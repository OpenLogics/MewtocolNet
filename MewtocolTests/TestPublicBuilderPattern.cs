using MewtocolNet;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.RegisterBuilding.BuilderPatterns;
using MewtocolNet.Registers;
using MewtocolTests.EncapsulatedTests;
using System.Collections;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests;

public class TestPublicBuilderPattern {

    private readonly ITestOutputHelper output;

    public TestPublicBuilderPattern(ITestOutputHelper output) => this.output = output;

    private void TestStruct<T> (string buildAddr, uint expectAddr, uint expectByteSize) where T : struct {

        using var interf = (MewtocolInterface)Mewtocol.Ethernet("192.168.115.210").Build();
        var builder = new RBuild(interf);

        var comparer = new StructRegister<T>(expectAddr, expectByteSize) {
            attachedInterface = interf,
            pollLevel = 1,
        };

        //test building to the internal list
        builder.Struct<T>(buildAddr).Build();
        var generated = builder.assembler.assembled.First();
        Assert.Equivalent(comparer, generated);
        builder.assembler.assembled.Clear();
        output.WriteLine(generated.Explain());

        //test building with direct out
        builder.Struct<T>(buildAddr).Build(out var testRef);
        Assert.Equivalent(comparer, testRef);
        builder.assembler.assembled.Clear();
        output.WriteLine(((Register)testRef).Explain());

        comparer.pollLevel++;

        //test building to the internal list with poll level
        builder.Struct<T>(buildAddr).PollLevel(2).Build();
        var generated2 = builder.assembler.assembled.First();
        Assert.Equivalent(comparer, generated2);
        builder.assembler.assembled.Clear();
        output.WriteLine(generated2.Explain());

        //test building direct out with poll level
        builder.Struct<T>(buildAddr).PollLevel(2).Build(out var testRef2);
        Assert.Equivalent(comparer, testRef2);
        builder.assembler.assembled.Clear();
        output.WriteLine(((Register)testRef2).Explain());

    }

    //16 bit structs

    [Fact(DisplayName = "[16 Bit] short")]
    public void TestStruct_1() => TestStruct<short>("DT100", 100, 2);

    [Fact(DisplayName = "[16 Bit] ushort")]
    public void TestStruct_2() => TestStruct<ushort>("DT101", 101, 2);

    [Fact(DisplayName = "[16 Bit] Word")]
    public void TestStruct_3() => TestStruct<Word>("DT102", 102, 2);

    [Fact(DisplayName = "[16 Bit] Enum")]
    public void TestStruct_4() => TestStruct<CurrentState16>("DT103", 103, 2);

    //32 bit structs

    [Fact(DisplayName = "[32 Bit] int")]
    public void TestStruct_5() => TestStruct<int>("DT104", 104, 4);

    [Fact(DisplayName = "[32 Bit] uint")]
    public void TestStruct_6() => TestStruct<uint>("DT105", 105, 4);

    [Fact(DisplayName = "[32 Bit] DWord")]
    public void TestStruct_7() => TestStruct<DWord>("DT106", 106, 4);

    [Fact(DisplayName = "[32 Bit] Enum")]
    public void TestStruct_8() => TestStruct<CurrentState32>("DT107", 107, 4);

    [Fact(DisplayName = "[32 Bit] TimeSpan")]
    public void TestStruct_9() => TestStruct<TimeSpan>("DT108", 108, 4);

}
