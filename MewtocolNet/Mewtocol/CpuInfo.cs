using System;

namespace MewtocolNet.Registers {
    public partial class CpuInfo {

        public CpuType Cputype { get; set; }
        public int ProgramCapacity { get; set; }
        public string CpuVersion { get; set; }


        public static CpuInfo BuildFromHexString (string _cpuType, string _cpuVersion, string _progCapacity) {

            CpuInfo retInf = new CpuInfo();

            switch (_cpuType) {
                case "02":
                retInf.Cputype = CpuType.FP5_16K;
                break;
                case "03":
                retInf.Cputype = CpuType.FP3_C_10K;
                break;
                case "04":
                retInf.Cputype = CpuType.FP1_M_0_9K;
                break;
                case "05":
                retInf.Cputype = CpuType.FP0_FP1_2_7K;
                break;
                case "06":
                retInf.Cputype = CpuType.FP0_FP1_5K_10K;
                break;
                case "12":
                retInf.Cputype = CpuType.FP5_24K;
                break;
                case "13":
                retInf.Cputype = CpuType.FP3_C_16K;
                break;
                case "20":
                retInf.Cputype = CpuType.FP_Sigma_X_H_30K_60K_120K;
                break;
                case "50":
                retInf.Cputype = CpuType.FP2_16K_32K;
                break;
            }

            retInf.ProgramCapacity = Convert.ToInt32(_progCapacity);
            retInf.CpuVersion = _cpuVersion.Insert(1, ".");
            return retInf;

        }
    }



}