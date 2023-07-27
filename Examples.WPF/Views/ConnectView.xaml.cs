using Examples.WPF.ViewModels;
using MewtocolNet;
using MewtocolNet.ComCassette;
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

            App.ViewModel.Plc = Mewtocol.Ethernet(viewModel.SelectedIP, parsedInt)
            .WithPoller()
            .WithRegisters(b => {
                b.Struct<short>("DT0").Build();
                b.Struct<short>("DT0").AsArray(30).Build();
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
