using MewtocolNet;

namespace MewtocolTests.EncapsulatedTests;

public class ExpectedPlcInformationData {

    public string PLCName { get; set; }

    public string PLCIP { get; set; }

    public int PLCPort { get; set; }

    public CpuType Type { get; set; }

    public int ProgCapacity { get; set; }

}
