using System;

namespace MewtocolNet.Registers {

    /// <summary>
    /// All modes
    /// </summary>
    public class PLCMode {
        public bool RunMode { get; set; }
        public bool TestRunMode { get; set; }
        public bool BreakExcecuting { get; set; }
        public bool BreakValid { get; set; }
        public bool OutputEnabled { get; set; }
        public bool StepRunMode { get; set; }
        public bool MessageExecuting { get; set; }
        public bool RemoteMode { get; set; }

        /// <summary>
        /// Gets operation mode from 2 digit hex number
        /// </summary>
        internal static PLCMode BuildFromHex (string _hexString) {

            string lower = Convert.ToString(Convert.ToInt32(_hexString.Substring(0, 1)), 2).PadLeft(4, '0');
            string higher = Convert.ToString(Convert.ToInt32(_hexString.Substring(1, 1)), 2).PadLeft(4, '0');
            string combined = lower + higher;

            var retMode = new PLCMode();

            for (int i = 0; i < 8; i++) {
                char digit = combined[i];
                bool state = false;
                if (digit.ToString() == "1")
                    state = true;
                switch (i) {
                    case 0:
                    retMode.RunMode = state;
                    break;
                    case 1:
                    retMode.TestRunMode = state;
                    break;
                    case 2:
                    retMode.BreakExcecuting = state;
                    break;
                    case 3:
                    retMode.BreakValid = state;
                    break;
                    case 4:
                    retMode.OutputEnabled = state;
                    break;
                    case 5:
                    retMode.StepRunMode = state;
                    break;
                    case 6:
                    retMode.MessageExecuting = state;
                    break;
                    case 7:
                    retMode.RemoteMode = state;
                    break;
                }
            }

            return retMode;

        }
    }

}