using System;

namespace MewtocolNet.Events {

    public delegate void PlcConnectionEventHandler(object sender, PlcConnectionArgs e);

    public class PlcConnectionArgs : EventArgs { }

}
