using MewtocolNet.Documentation;

namespace MewtocolNet {

    //this overwrites the CPU code and only comes with EXRT
    //special chars: (d = -) (c = .) (s = /)

    //MISSING! FP7 and EcoLogix

    // Byte layout explained:
    // PLCs that are identifiable by just one byte have just one byte as their code 0x00
    // PLCs that are not identifiable by just one byte and also need a prog size param to identfy use the second byte for capacity 0x32_00 => 32k of type 00
    // PLCs that use the mewtocol 7 identifcation method (currently only fp7 and EL500) use the third byte for their inital identifier (07 for fp7) => 0x07_00_09
    // Sometimes the type is no directly identifable, then two types share the same code and make logically no difference

    //special codes for mem size are: 02 = 2.5k, 03 = 2.7k

    /// <summary>
    /// Type identifier of the plc
    /// </summary>
    public enum PlcType {

        #region FP5 Family (Legacy)

        /// <summary>
        /// FP5 16k
        /// </summary>
        [PlcLegacy]
        FP5_16k = 0x10_02,

        /// <summary>
        /// FP5 24k
        /// </summary>
        [PlcLegacy]
        FP5_24k = 0x18_12,

        #endregion

        #region FP2 Family (Legacy)

        /// <summary>
        /// FP2 16k
        /// </summary>
        [PlcLegacy]
        FP2_16k = 0x10_50,

        /// <summary>
        /// FP2 32k
        /// </summary>
        [PlcLegacy]
        FP2_32k = 0x20_50,

        #endregion

        #region FP3/FP-C Family (Legacy)

        /// <summary>
        /// FP3 10k
        /// </summary>
        [PlcLegacy]
        FP3_10k = 0x0A_03,
        /// <summary>
        /// FP-C 16k
        /// </summary>
        [PlcLegacy]
        FPdC_16k = 0x10_13,
        /// <summary>
        /// FP3
        /// </summary>
        [PlcLegacy]
        FP3_16k = FPdC_16k,
        
        #endregion

        #region FP1 / FPM Family (Legacy)

        /// <summary>
        /// FP1 0.9k C14,C16
        /// </summary>
        [PlcLegacy]
        FP1_0c9k__C14_C16 = 0x00_04,
        /// <summary>
        /// FP-M 0.9k C16T
        /// </summary>
        [PlcLegacy]
        FPdM_0c9k__C16T = FP1_0c9k__C14_C16,
        /// <summary>
        /// FP1 2.7k C24,C40
        /// </summary>
        [PlcLegacy]
        FP1_2c7k__C24_C40 = 0x03_05,
        /// <summary>
        /// FP-M 2.7k C20R,C20T,C32T
        /// </summary>
        [PlcLegacy]
        FPdM_2c7k__C20R_C20T_C32T = FP1_2c7k__C24_C40,
        /// <summary>
        /// FP1 5.0k C56,C72
        /// </summary>
        [PlcLegacy]
        FP1_5k__C56_C72 = 0x00_06,
        /// <summary>
        /// FPM 5.0k C20RC,C20TC,C32TC
        /// </summary>
        [PlcLegacy]
        FPdM_5k__C20RC_C20TC_C32TC = FP1_5k__C56_C72,

        #endregion

        #region FP10 Family (Legacy)

        /// <summary>
        /// FP10S 30k
        /// </summary>
        [PlcLegacy]
        FP10S_30k = 0x1E_20,
        /// <summary>
        /// FP10 30k
        /// </summary>
        [PlcLegacy]
        FP10_30k = FP10S_30k,
        /// <summary>
        /// FP10 60k
        /// </summary>
        [PlcLegacy]
        FP10_60k = 0x3C_20,

        #endregion

        #region FP10SH Family (Legacy)

        /// <summary>
        /// FP10SH 30k
        /// </summary>
        [PlcLegacy]
        FP10SH_30k = 0x1E_30,
        /// <summary>
        /// FP10SH 60k
        /// </summary>
        [PlcLegacy]
        FP10SH_60k = 0x3C_30,
        /// <summary>
        /// FP10SH 120k
        /// </summary>
        [PlcLegacy]
        FP10SH_120k = 0x78_30,

        #endregion

        #region FP0 Family (Legacy)

        /// <summary>
        /// FP0 2.7k C10,C14,C16
        /// </summary>
        [PlcLegacy]
        FP0_2c7k__C10_C14_C16 = 0x03_40,
        /// <summary>
        /// FP0 5k
        /// </summary>
        [PlcLegacy]
        FP0_5k__C32_SL1 = 0x00_41,
        /// <summary>
        /// FP0 10k
        /// </summary>
        [PlcLegacy]
        FP0_10c0k__T32 = 0x0A_42,

        #endregion

        #region FP-Sigma Family (Legacy)

        /// <summary>
        /// FP-SIGMA 12k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdSIGMA_12k = 0x0C_43,
        /// <summary>
        /// FP-SIGMA 32k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdSIGMA_32k = 0x20_44,
        /// <summary>
        /// FP-SIGMA 16k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdSIGMA_16k = 0x10_E1,
        /// <summary>
        /// FP-SIGMA 40k (never supported)
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdSIGMA_40k = 0x28_E1,

        #endregion

        #region FP-e Family (Legacy)

        /// <summary>
        /// FP-e 2.7k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPde_2c7k = 0x03_45,

        #endregion

        #region FP0R Family

        /// <summary>
        /// FP0R 16k C10,C14,C16
        /// </summary>
        [PlcEXRT]
        FP0R_16k__C10_C14_C16 = 0x46,
        /// <summary>
        /// FP0R 32k C32
        /// </summary>
        [PlcEXRT]
        FP0R_32k__C32 = 0x20_47,
        /// <summary>
        /// FP0R 32k T32
        /// </summary>
        [PlcEXRT]
        FP0R_32k__T32 = 0x20_48,
        /// <summary>
        /// FP0R 32k F32
        /// </summary>
        [PlcEXRT]
        FP0R_32k__F32 = 0x20_49,

        #endregion

        #region FP2SH Family (Legacy)

        /// <summary>
        /// FP2SH 32k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FP2SH_32k = 0x20_62,
        /// <summary>
        /// FP2SH 60k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FP2SH_60k = 0x3C_60,
        /// <summary>
        /// FP2SH 120k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FP2SH_120k = 0x78_E0,

        #endregion

        #region FP-X Family (Legacy)

        /// <summary>
        /// FP-X 16k C14R
        /// </summary>
        [PlcLegacy, PlcEXRT, PlcCodeTested]
        FPdX_16k__C14R = 0x10_70,
        /// <summary>
        /// FP-X 32k C30R,C60R
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_32k__C30R_C60R = 0x20_71,
        /// <summary>
        /// FP-X0 2.5k L14,L30
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX0_2c5k__L14_L30 = 0x02_72,
        /// <summary>
        /// FP-X 16k L14
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_16k__L14 = 0x10_73,
        /// <summary>
        /// FP-X 32k L30,L60
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_32k__L30_L60 = 0x20_74,
        /// <summary>
        /// FP-X0 8k L40,L60
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX0_8k__L40_L60 = 0x08_75,
        /// <summary>
        /// FP-X 16k C14T/P
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_16k__C14TsP = 0x10_76,
        /// <summary>
        /// FP-X 32k C30T/P,C60T/P,C38AT,C40T
        /// </summary>
        [PlcLegacy, PlcEXRT, PlcCodeTested]
        FPdX_32k__C30TsP_C60TsP_C38AT_C40T = 0x20_77,
        /// <summary>
        /// FP-X 2.5k C40RT0A
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_2c5k__C40RT0A = 0x02_7A,
        /// <summary>
        /// FP-X0 16k L40,L60
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX0_16k__L40_L60 = 0x10_7F,

        #endregion

        #region FP-XH Family

        /// <summary>
        /// FP-XH 16k C14R
        /// </summary>
        [PlcEXRT, PlcCodeTested]
        FPdXH_16k__C14R = 0x10_A0,
        /// <summary>
        /// FP-XH 32k C30R,C40R,C60R
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__C30R_C40R_C60R = 0x20_A1,
        /// <summary>
        /// FP-XH 16k C14T/P
        /// </summary>
        [PlcEXRT]
        FPdXH_16k__C14TsP = 0x10_A4,
        /// <summary>
        /// FP-XH 32k C30T/P,C40T,C60T/P
        /// </summary>
        [PlcEXRT, PlcCodeTested]
        FPdXH_32k__C30TsP_C40T_C60TsP = 0x20_A5,
        /// <summary>
        /// FP-XH 32k C38AT
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__C38AT = 0x20_A7,
        /// <summary>
        /// FP-XH 32k M4T/L
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__M4TsL = 0x20_A8,
        /// <summary>
        /// FP-XH 32k M8N16T/P (RTEX)
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__M8N16TsP = 0x20_AC,
        /// <summary>
        /// FP-XH 32k M8N30T (RTEX)
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__M8N30T = 0x20_AD,
        /// <summary>
        /// FP-XH 32k C40ET,C60ET
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__C40ET_C60ET = 0x20_AE,
        /// <summary>
        /// FP-XH 32k C60ETF
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__C60ETF = 0x20_AF,

        #endregion

        #region FP0H Family

        /// <summary>
        /// FP0H 32k C32T/P
        /// </summary>
        [PlcEXRT]
        FP0H_32k__C32TsP = 0x20_B0,
        /// <summary>
        /// FP0H 32k C32ET/EP
        /// </summary>
        [PlcEXRT]
        FP0H_32k__C32ETsEP = 0x20_B1,

        #endregion

        #region FP7 Family

        /// <summary>
        /// FP7 CPS41E (Series code 7)
        /// </summary>
        FP7_196k__CPS41E = 0x07_C4_03,
        /// <summary>
        /// FP7 CPS31E (Series code 7)
        /// </summary>
        FP7_120k__CPS31E = 0x07_78_04,
        /// <summary>
        /// FP7 CPS31 (Series code 7)
        /// </summary>
        FP7_120k__CPS31 = 0x07_78_05,
        /// <summary>
        /// FP7 CPS41ES (Series code 7)
        /// </summary>
        FP7_196k__CPS41ES = 0x07_C4_06,
        /// <summary>
        /// FP7 CPS31ES (Series code 7)
        /// </summary>
        FP7_120k__CPS31ES = 0x07_78_07,
        /// <summary>
        /// FP7 CPS31S (Series code 7)
        /// </summary>
        FP7_120k__CPS31S = 0x07_78_08,
        /// <summary>
        /// FP7 CPS21 (Series code 7)
        /// </summary>
        FP7_64k__CPS21 = 0x07_40_09,

        #endregion

        #region EcoLogicX Family

        /// <summary>
        /// EcoLogiX (Series code 7)
        /// </summary>
        [PlcLegacy]
        ECOLOGIX_0k__ELC500 = 0x07_00_10,

        #endregion

    }

}