using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.RegisterAttributes {

    public class RegisterCollectionBase : INotifyPropertyChanged {

        public MewtocolInterface PLCInterface { get; set; }        

        public event PropertyChangedEventHandler PropertyChanged;

        internal void TriggerPropertyChanged (string propertyName = null) {
            var handler = PropertyChanged;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
