using MewtocolNet.DocAttributes;

namespace MewtocolNet {

    //this overwrites the CPU code and only comes with EXRT
    //special chars: (d = -) (c = .) (s = /)

    //MISSING! FP7 and EcoLogix

    /// <summary>
    /// The type of the PLC
    /// </summary>
    public enum PlcType {

        #region FP5 Family (Legacy)

        /// <summary>
        /// FP5 16k
        /// </summary>
        [PlcLegacy]
        FP5_16k = 0x02,

        /// <summary>
        /// FP5 24k
        /// </summary>
        [PlcLegacy]
        FP5_24k = 0x12,

        #endregion

        #region FP2 Family (Legacy)

        /// <summary>
        /// FP2 16k OR FP2 32k
        /// </summary>
        [PlcLegacy]
        FP2_16k_OR_FP2_32k = 0x50,

        //misses entry FP2 32k

        #endregion

        #region FP3/FP-C Family (Legacy)

        /// <summary>
        /// FP3 10k
        /// </summary>
        [PlcLegacy]
        FP3_10k = 0x03,
        /// <summary>
        /// FP3 or FP-C 16k
        /// </summary>
        [PlcLegacy]
        FP3_16k_OR_FPdC_16k = 0x13,

        #endregion

        #region FP1 / FPM Family (Legacy)

        /// <summary>
        /// FP1 0.9k C14,C16 or FP-M 0.9k C16T
        /// </summary>
        [PlcLegacy]
        FP1_0c9k__C14_C16_OR_FPdM_0c9k__C16T = 0x04,
        /// <summary>
        /// FP1 2.7k C24,C40 or FP-M 2.7k C20R,C20T,C32T
        /// </summary>
        [PlcLegacy]
        FP1_2c7k__C24_C40_OR_FPdM_2c7k__C20R_C20T_C32T = 0x05,
        /// <summary>
        /// FP1 5.0k C56,C72 or FPM 5k C20RC,C20TC,C32TC
        /// </summary>
        [PlcLegacy]
        FP1_5k__C56_C72_OR_FPdM_5k__C20RC_C20TC_C32TC = 0x06,

        #endregion

        #region FP10 Family (Legacy)

        /// <summary>
        /// FP10 30k,60k OR FP10S 30k
        /// </summary>
        [PlcLegacy]
        FP10_30k_OR_FP10_60k_OR_FP10S_30k = 0x20,

        //misses entry FP10 60k 

        #endregion

        #region FP10SH Family (Legacy)

        /// <summary>
        /// FP10SH 30k, 60k, 120k
        /// </summary>
        [PlcLegacy]
        FP10SH_30k_OR_FP10SH_60k_OR_FP10SH_120k = 0x30,

        #endregion

        #region FP0 Family (Legacy)

        /// <summary>
        /// FP0 2.7k C10,C14,C16
        /// </summary>
        [PlcLegacy]
        FP0_2c7k__C10_C14_C16 = 0x40,
        /// <summary>
        /// FP0 5k
        /// </summary>
        [PlcLegacy]
        FP0_5k__C32_SL1 = 0x41,
        /// <summary>
        /// FP0 10k
        /// </summary>
        [PlcLegacy]
        FP0_10c0k__T32 = 0x42,

        #endregion

        #region FP-Sigma Family (Legacy)

        /// <summary>
        /// FP-SIGMA 12k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdSIGMA_12k = 0x43,
        /// <summary>
        /// FP-SIGMA 32k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdSIGMA_32k = 0x44,
        /// <summary>
        /// FP-SIGMA 16k or FP-SIGMA 40k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdSIGMA_16k_OR_FPdSIGMA_40k = 0xE1,

        #endregion

        #region FP-e Family (Legacy)

        /// <summary>
        /// FP-e 2.7k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPde_2c7k = 0x45,

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
        FP0R_32k__C32 = 0x47,
        /// <summary>
        /// FP0R 32k T32
        /// </summary>
        [PlcEXRT]
        FP0R_32k__T32 = 0x48,
        /// <summary>
        /// FP0R 32k F32
        /// </summary>
        [PlcEXRT]
        FP0R_32k__F32 = 0x49,

        #endregion

        #region FP2SH Family (Legacy)

        /// <summary>
        /// FP2SH 60k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FP2SH_60k = 0x60,
        /// <summary>
        /// FP2SH 32k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FP2SH_32k = 0x62,
        /// <summary>
        /// FP2SH 120k
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FP2SH_120k = 0xE0,

        #endregion

        #region FP-X Family (Legacy)

        /// <summary>
        /// FP-X 16k C14R
        /// </summary>
        [PlcLegacy, PlcEXRT, PlcCodeTested]
        FPdX_16k__C14R = 0x70,
        /// <summary>
        /// FP-X 32k C30R,C60R
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_32k__C30R_C60R = 0x71,
        /// <summary>
        /// FP-X0 2.5k L14,L30
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX0_2c5k__L14_L30 = 0x72,
        /// <summary>
        /// FP-X 16k L14
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_16k__L14 = 0x73,
        /// <summary>
        /// FP-X 32k L30,L60
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_32k__L30_L60 = 0x74,
        /// <summary>
        /// FP-X0 8k L40,L60
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX0_8k__L40_L60 = 0x75,
        /// <summary>
        /// FP-X 16k C14T/P
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_16k__C14TsP = 0x76,
        /// <summary>
        /// FP-X 32k C30T/P,C60T/P,C38AT,C40T
        /// </summary>
        [PlcLegacy, PlcEXRT, PlcCodeTested]
        FPdX_32k__C30TsP_C60TsP_C38AT_C40T = 0x77,
        /// <summary>
        /// FP-X 2.5k C40RT0A
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX_2c5k__C40RT0A = 0x7A,
        /// <summary>
        /// FP-X0 16k L40,L60
        /// </summary>
        [PlcLegacy, PlcEXRT]
        FPdX0_16k__L40_L60 = 0x7F,

        #endregion

        #region FP-XH Family

        /// <summary>
        /// FP-XH 16k C14R
        /// </summary>
        [PlcEXRT, PlcCodeTested]
        FPdXH_16k__C14R = 0xA0,
        /// <summary>
        /// FP-XH 32k C30R,C40R,C60R
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__C30R_C40R_C60R = 0xA1,
        /// <summary>
        /// FP-XH 16k C14T/P
        /// </summary>
        [PlcEXRT]
        FPdXH_16k__C14TsP = 0xA4,
        /// <summary>
        /// FP-XH 32k C30T/P,C40T,C60T/P
        /// </summary>
        [PlcEXRT, PlcCodeTested]
        FPdXH_32k__C30TsP_C40T_C60TsP = 0xA5,
        /// <summary>
        /// FP-XH 32k C38AT
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__C38AT = 0xA7,
        /// <summary>
        /// FP-XH 32k M4T/L
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__M4TsL = 0xA8,
        /// <summary>
        /// FP-XH 32k M8N16T/P (RTEX)
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__M8N16TsP = 0xAC,
        /// <summary>
        /// FP-XH 32k M8N30T (RTEX)
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__M8N30T = 0xAD,
        /// <summary>
        /// FP-XH 32k C40ET,C60ET
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__C40ET_C60ET = 0xAE,
        /// <summary>
        /// FP-XH 32k C60ETF
        /// </summary>
        [PlcEXRT]
        FPdXH_32k__C60ETF = 0xAF,

        #endregion

        #region FP0H Family

        /// <summary>
        /// FP0H 32k C32T/P
        /// </summary>
        [PlcEXRT]
        FP0H_32k__C32TsP = 0xB0,
        /// <summary>
        /// FP0H 32k C32ET/EP
        /// </summary>
        [PlcEXRT]
        FP0H_32k__C32ETsEP = 0xB1,

        #endregion

    }

}