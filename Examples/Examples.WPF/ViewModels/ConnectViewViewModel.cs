using System;
using System.Collections.Generic;
using System.Configuration.Internal;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using MewtocolNet;
using MewtocolNet.ComCassette;

namespace Examples.WPF.ViewModels;

internal class ConnectViewViewModel : ViewModelBase {

    private bool hasComports = false;   
    private IEnumerable<int> baudRates = null!;
    private IEnumerable<string> comPorts = null!;
    private IEnumerable<CassetteInformation> foundCassettes = null!;

    private string selectedIP = "192.168.115.210";
    private string selectedPort = "9094";
    private bool isConnecting;

    public IEnumerable<int> BaudRates { 
        get => baudRates; 
        set {
            baudRates = value;
            OnPropChange();
        }
    }

    public IEnumerable<string> ComPorts {
        get => comPorts;
        set {
            comPorts = value;
            OnPropChange();
        }
    }

    public IEnumerable<CassetteInformation> FoundCassettes {
        get => foundCassettes;
        set {
            foundCassettes = value;
            OnPropChange();
        }
    }

    public bool HasComports {
        get => hasComports;
        set {
            hasComports = value;
            OnPropChange();
        }
    }

    public string SelectedIP {
        get { return selectedIP; }
        set {
            selectedIP = value;
            OnPropChange();
        }
    }


    public string SelectedPort {
        get { return selectedPort; }
        set {
            selectedPort = value;
            OnPropChange();
        }
    }

    public bool IsConnecting {
        get { return isConnecting; }
        set { 
            isConnecting = value;
            OnPropChange();
        }
    }

    private DispatcherTimer tm;

    public ConnectViewViewModel() {

        BaudRates = Mewtocol.GetUseableBaudRates();
        ScanTimerTick(null, null!);

        tm = new DispatcherTimer {
            Interval = TimeSpan.FromSeconds(3),
        };
        tm.Tick += ScanTimerTick;

        tm.Start();

    }

    private async void ScanTimerTick(object? sender, EventArgs e) {

        ComPorts = Mewtocol.GetSerialPortNames();
        HasComports = ComPorts != null && ComPorts.Count() > 0;

        var found = await CassetteFinder.FindClientsAsync(timeoutMs: 1000);
        if (FoundCassettes == null || !Enumerable.SequenceEqual(found, FoundCassettes))
            FoundCassettes = found;

    }

    internal void SelectedCassette (CassetteInformation cassette) {

        SelectedIP = cassette.IPAddress.ToString();
        SelectedPort = cassette.Port.ToString();

    }

    internal void EndTimer() => tm.Stop();

}
