namespace MewtocolNet {

    /// <summary>
    /// CPU type of the PLC
    /// </summary>
    public enum CpuType {
        /// <summary>
        /// FP 0 / FP 2.7K
        /// </summary>
        FP0_FP1_2_7K,
        /// <summary>
        /// FP0 / FP1, 5K / 10K
        /// </summary>
        FP0_FP1_5K_10K,
        /// <summary>
        /// FP1 M 0.9K
        /// </summary>
        FP1_M_0_9K,
        /// <summary>
        /// FP2 16k / 32k
        /// </summary>
        FP2_16K_32K,
        /// <summary>
        /// FP3 C 10K 
        /// </summary>
        FP3_C_10K,
        /// <summary>
        /// FP3 C 16K
        /// </summary>
        FP3_C_16K,
        /// <summary>
        /// FP5 16K
        /// </summary>
        FP5_16K,
        /// <summary>
        /// FP 5 24K
        /// </summary>
        FP5_24K,
        /// <summary>
        /// Includes panasonic FPX, FPX-H, Sigma
        /// </summary>
        FP_Sigma_X_H_30K_60K_120K

    }

}