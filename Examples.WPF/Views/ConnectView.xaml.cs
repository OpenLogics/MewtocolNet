using Examples.WPF.ViewModels;
using MewtocolNet;
using MewtocolNet.ComCassette;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Examples.WPF.Views;

/// <summary>
/// Interaktionslogik für ConnectView.xaml
/// </summary>
public partial class ConnectView : UserControl {

    private ConnectViewViewModel viewModel;

    public ConnectView() {

        InitializeComponent();
        viewModel = new ConnectViewViewModel();
        this.DataContext = viewModel;

        Unloaded += (s, e) => viewModel.EndTimer();

    }

    private void SelectedCassette(object sender, SelectionChangedEventArgs e) {

        var cassette = (CassetteInformation)((DataGrid)sender).SelectedItem;
        if (cassette == null) return;

        viewModel.SelectedCassette(cassette);

    }

    private void ClickedConnectEth(object sender, RoutedEventArgs e) {

        Application.Current.Dispatcher.BeginInvoke(async () => {

            viewModel.IsConnecting = true;

            var parsedInt = int.Parse(viewModel.SelectedPort);

            IRegister<short> heartbeatSetter = null!;

            App.ViewModel.Plc = Mewtocol.Ethernet(viewModel.SelectedIP, parsedInt)
            .WithPoller()
            .WithInterfaceSettings(setting => {
                setting.TryReconnectAttempts = 0;
                setting.TryReconnectDelayMs = 2000;
                setting.SendReceiveTimeoutMs = 1000;
                setting.HeartbeatIntervalMs = 3000;
                setting.MaxDataBlocksPerWrite = 12;
                setting.MaxOptimizationDistance = 10;
            })
            .WithCustomPollLevels(lvl => {
                lvl.SetLevel(2, 3);
                lvl.SetLevel(3, TimeSpan.FromSeconds(5));
                lvl.SetLevel(4, TimeSpan.FromSeconds(10));
            })
            .WithRegisters(b => {

                //b.Struct<short>("DT0").Build();
                //b.Struct<short>("DT0").AsArray(30).Build();

                b.Bool("R10A").Build();

                b.Struct<short>("DT1000").Build(out heartbeatSetter);
                b.Struct<Word>("DT1000").Build();

                b.Struct<ushort>("DT1001").PollLevel(2).Build();
                b.Struct<Word>("DT1002").PollLevel(2).Build();

                b.Struct<int>("DDT1010").PollLevel(2).Build();
                b.Struct<uint>("DDT1012").PollLevel(2).Build();
                b.Struct<DWord>("DDT1014").PollLevel(2).Build();
                b.Struct<float>("DDT1016").PollLevel(2).Build();
                b.Struct<TimeSpan>("DDT1018").PollLevel(2).Build();

                b.String("DT1024", 32).PollLevel(3).Build();
                b.String("DT1042", 5).PollLevel(4).Build();

            })
            .WithHeartbeatTask(async () => {

                await heartbeatSetter.WriteAsync((short)new Random().Next(short.MinValue, short.MaxValue));

            })
            .Build();

            await App.ViewModel.Plc.ConnectAsync();

            if (App.ViewModel.Plc.IsConnected) {

                App.MainWindow.mainContent.Content = new PlcDataView();

            }

            viewModel.IsConnecting = false;

        }, System.Windows.Threading.DispatcherPriority.Send);

    }

}
