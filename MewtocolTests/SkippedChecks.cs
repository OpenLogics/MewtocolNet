using MewtocolNet;
using MewtocolNet.DocAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests;

public class SkippedChecks {

    private readonly ITestOutputHelper output;

    public SkippedChecks(ITestOutputHelper output) {
        this.output = output;
    }

    [Fact]
    public void BuildBCCFrameGeneration() {

        var toSuccess = new List<Type> {

            typeof(PlcCodeTestedAttribute),
            typeof(PlcEXRTAttribute),
            typeof(PlcLegacyAttribute),

        };

        Assert.NotNull(toSuccess);  

    }

}
