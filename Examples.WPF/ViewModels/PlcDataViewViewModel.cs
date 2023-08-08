using MewtocolNet;
using MewtocolNet.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.WPF.ViewModels;

public class PlcDataViewViewModel : ViewModelBase {

    private ReconnectArgs plcCurrentReconnectArgs = null!;

    public IPlc Plc => App.ViewModel.Plc!;

    public ReconnectArgs PlcCurrentReconnectArgs { 
        get => plcCurrentReconnectArgs; 
        set {
            plcCurrentReconnectArgs = value;
            OnPropChange();
        }
    }

    public PlcDataViewViewModel () {

        Plc.ReconnectTryStarted += (s, e) => PlcCurrentReconnectArgs = e;
        Plc.Reconnected += (s, e) => PlcCurrentReconnectArgs = null!;
        Plc.Disconnected += (s, e) => PlcCurrentReconnectArgs = null!;

    }

}