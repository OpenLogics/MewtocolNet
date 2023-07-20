using MewtocolNet;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.RegisterBuilding.BuilderPatterns;
using MewtocolNet.Registers;
using MewtocolTests.EncapsulatedTests;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests;

public class TestPublicBuilderPatternArray {

    private readonly ITestOutputHelper output;

    public TestPublicBuilderPatternArray(ITestOutputHelper output) => this.output = output;

    private void TestArray1D<T> (string buildAddr, int indices1, uint expectAddr, uint expectByteSize) where T : struct {

        using var interf = (MewtocolInterface)Mewtocol.Ethernet("192.168.115.210").Build();
        var builder = new RBuild(interf);

        var comparer = new ArrayRegister<T>(expectAddr, expectByteSize, new int[] { indices1 }) {
            attachedInterface = interf,
            pollLevel = 1
        };

        //test building to the internal list
        builder.Struct<T>(buildAddr).AsArray(indices1).Build();
        var generated = builder.assembler.assembled.First();
        
        Assert.Equivalent(comparer, generated);

        builder.assembler.assembled.Clear();
        output.WriteLine(generated.Explain());

        ////test building with direct out
        //builder.Struct<T>(buildAddr).AsArray(indices1).Build(out var testRef);
        //Assert.Equivalent(comparer, testRef);
        //builder.assembler.assembled.Clear();
        //output.WriteLine(((Register)testRef).Explain());

        //comparer.pollLevel++;

        ////test building to the internal list with poll level
        //builder.Struct<T>(buildAddr).AsArray(indices1).PollLevel(2).Build();
        //var generated2 = builder.assembler.assembled.First();
        //Assert.Equivalent(comparer, generated2);
        //builder.assembler.assembled.Clear();
        //output.WriteLine(generated2.Explain());

        ////test building direct out with poll level
        //builder.Struct<T>(buildAddr).AsArray(indices1).PollLevel(2).Build(out var testRef2);
        //Assert.Equivalent(comparer, testRef2);
        //builder.assembler.assembled.Clear();
        //output.WriteLine(((Register)testRef2).Explain());

    }

    //16 bit structs

}
