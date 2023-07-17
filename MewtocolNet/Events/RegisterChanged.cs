using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.Events {

    public delegate void RegisterChangedEventHandler(object sender, RegisterChangedArgs e);

    public class RegisterChangedArgs : EventArgs {

        public IRegister Register { get; internal set; }

        public object Value { get; internal set; }   
        
        public object PreviousValue { get; internal set; }

        public string PreviousValueString { get; internal set; }

    }

}
