using System;

namespace MewtocolNet.Events {

    public delegate void PlcModeChangedEventHandler(object sender, PlcModeArgs e);

    public class PlcModeArgs : EventArgs {

        public OPMode LastMode { get; internal set; }

        public OPMode NowMode { get; internal set; }

        public bool ProgToRun { get; internal set; } 

        public bool RunToProg { get; internal set; }

    }

}
