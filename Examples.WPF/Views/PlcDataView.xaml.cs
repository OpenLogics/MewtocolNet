using Examples.WPF.ViewModels;
using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
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

public partial class PlcDataView : UserControl {

    private PlcDataViewViewModel viewModel;

    public PlcDataView() {
        
        InitializeComponent();

        viewModel = new PlcDataViewViewModel();
        this.DataContext = viewModel;

    }

    private void ClickedDisconnect(object sender, RoutedEventArgs e) {

        viewModel.Plc.Disconnect();

    }

    private async void ClickedConnect(object sender, RoutedEventArgs e) {

        await viewModel.Plc.ConnectAsync();

    }

    private async void ClickedSetRandom(object sender, RoutedEventArgs e) {

        var reg = (IRegister<ushort>?)viewModel.Plc.GetAllRegisters()?.FirstOrDefault(x => x.PLCAddressName == "DT1001");

        if(reg != null) {

            await reg.WriteAsync((ushort)new Random().Next(ushort.MinValue, ushort.MaxValue));

        }

    }

    private async void ClickedAddQueueTest(object sender, RoutedEventArgs e) {

        var tasks = new List<Task<short>>();

        for (int i = 0; i < 100; i++) {

            var t = viewModel.Plc.Register.Struct<short>("DT1000").ReadAsync();

            tasks.Add(t);

        }

        var list = await Task.WhenAll(tasks);

        Console.WriteLine();

    }

    private async void ClickedToggleRunMode(object sender, RoutedEventArgs e) {

        await viewModel.Plc.ToggleOperationModeAsync();

    }

}
