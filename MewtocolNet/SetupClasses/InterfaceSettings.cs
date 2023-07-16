namespace MewtocolNet.SetupClasses {

    public class InterfaceSettings {

        /// <summary>
        /// <code>
        /// This feature can improve read write times by a big margin but also
        /// block outgoing messages inbetween polling cycles more frequently
        /// </code>
        /// The max distance of the gap between registers (if there is a gap between 
        /// adjacent registers) to merge them into one request <br/>
        /// Example: <br/>
        /// <example>
        /// We have a register at DT100 (1 word long) and a
        /// register at DT101 (1 word long) <br/>
        /// - If the max distance is 0 it will not merge them into one request<br/>
        /// - If the max distance is 1 it will merge them into one request<br/>
        /// - If the max distance is 2 and the next register is at DT102 it will also merge them and ignore the spacer byte in the response<br/>
        /// </example>
        /// </summary>

        public int MaxOptimizationDistance { get; set; } = 4;

        /// <summary>
        /// The overwrite mode for poll levels <br/>
        /// When set to <see cref="PollLevelOverwriteMode.Lowest"/> the lowest average poll level for overlapping registers gets used <br/>
        /// When set to <see cref="PollLevelOverwriteMode.Highest"/> the highest average poll level for overlapping registers gets used
        /// </summary>
        public PollLevelOverwriteMode PollLevelOverwriteMode { get; set; } = PollLevelOverwriteMode.Highest;

        /// <summary>
        /// Defines how many WORD blocks the interface will send on a DT area write request before splitting up messages <br/>
        /// Higher numbers will result in a longer send and receive thread blocking time
        /// </summary>
        public int MaxDataBlocksPerWrite { get; set; } = 8;

    }

}
