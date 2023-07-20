using MewtocolNet;
using MewtocolNet.Registers;
using MewtocolTests.EncapsulatedTests;
using Xunit;
using Xunit.Abstractions;

namespace MewtocolTests {

    public partial class AutomatedPropertyRegisters {

        private readonly ITestOutputHelper output;

        public AutomatedPropertyRegisters(ITestOutputHelper output) {
            this.output = output;
        }

    }

}