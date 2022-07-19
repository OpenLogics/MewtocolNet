﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.RegisterAttributes {

    /// <summary>
    /// A register collection base with full auto read and notification support built in
    /// </summary>
    public class RegisterCollectionBase : INotifyPropertyChanged {

        /// <summary>
        /// Reference to its bound interface
        /// </summary>
        public MewtocolInterface PLCInterface { get; set; }        

        /// <summary>
        /// Whenever one of its props changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Triggers a property changed event
        /// </summary>
        /// <param name="propertyName">Name of the property to trigger for</param>
        public void TriggerPropertyChanged (string propertyName = null) {
            var handler = PropertyChanged;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Gets called when the register collection base was linked to its parent mewtocol interface
        /// </summary>
        /// <param name="plc">The parent interface</param>
        public virtual void OnInterfaceLinked (MewtocolInterface plc) { }

        /// <summary>
        /// Gets called when the register collection base was linked to its parent mewtocol interface
        /// and the plc connection is established
        /// </summary>
        /// <param name="plc">The parent interface</param>
        public virtual void OnInterfaceLinkedAndOnline (MewtocolInterface plc) { }

    }

}
