using MewtocolNet.Registers;
using System.Collections.Generic;
using System.ComponentModel;

namespace MewtocolNet.UnderlyingRegisters {

    public interface IMemoryArea : INotifyPropertyChanged {

        string AddressRange { get; }

        IReadOnlyList<Word> UnderlyingWords { get; }    

        string UnderlyingWordsString { get; }   

        int PollLevel { get; }  

    }

}
