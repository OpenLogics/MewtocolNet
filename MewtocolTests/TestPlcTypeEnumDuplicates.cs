using MewtocolNet;
using Xunit;
using Xunit.Abstractions;

using MewtocolNet.Helpers;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace MewtocolTests {

    public class TestPlcTypeEnumDuplicates {

        private readonly List<string[]> allowedSynonims = new() {

            PlcType.FP1_0c9k__C14_C16.GetSynonims(),
            PlcType.FPdM_2c7k__C20R_C20T_C32T.GetSynonims(),
            PlcType.FPdM_5k__C20RC_C20TC_C32TC.GetSynonims(),
            PlcType.FPdC_16k.GetSynonims(),
            PlcType.FP10S_30k.GetSynonims(),

        };

        private readonly ITestOutputHelper output;

        public TestPlcTypeEnumDuplicates(ITestOutputHelper output) {
            this.output = output;
        }

        [Fact(DisplayName = "Check if the PLC type enums contain duplicates")]
        public void NumericRegisterMewtocolIdentifiers() {

            int nameCount = Enum.GetNames<PlcType>().Length;
            int enumCount = Enum.GetValues<PlcType>().Cast<int>().Distinct().Count();

            var groupedCodes = Enum.GetValues<PlcType>().Cast<int>().GroupBy(x => x);

            foreach (var item in groupedCodes) {

                if (item.Count() <= 1) continue;

                output.WriteLine($"Code: {item.Key.ToString("X6")}");

                var synonims = ((PlcType)item.Key).GetSynonims();

                var sononymousGroup = allowedSynonims.FirstOrDefault(x => x.Contains(synonims.First()));

                if (sononymousGroup == null) Assert.Fail($"The synonymous group doesn't exist ({synonims.First()})");

                Assert.Equal(sononymousGroup.OrderBy(x => x).ToArray(), synonims.OrderBy(x => x).ToArray());

                foreach (var syn in synonims) {

                    output.WriteLine($"Synonim: {syn}");
                
                }

            }

            output.WriteLine($"Indivual names: {nameCount}");
            output.WriteLine($"Indivual enums: {enumCount}");

        }
    
    }

}