using MewtocolNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests;

public class BCCBuilderChecks {

    private readonly ITestOutputHelper output;

    public BCCBuilderChecks (ITestOutputHelper output) {
        this.output = output;
    }

    [Fact(DisplayName = "Test CRC Generation (CRC-8)")]
    public void BuildBCCFrameGeneration() {

        string test = "%01#RCSX0000";
        string expect = "%01#RCSX00001D";

        Assert.Equal(expect, test.BCC_Mew());

    }

    [Fact(DisplayName = "Test CRC Generation (CRC-16/MCRF4XX)")]
    public void BuildBCC7FrameGeneration() {

        string test = ">@EEE00$30STRD00070300000453045304530100000000660100";
        string expect = ">@EEE00$30STRD00070300000453045304530100000000660100A7A5";

        Assert.Equal(expect, test.BCC_Mew7());

    }

}
