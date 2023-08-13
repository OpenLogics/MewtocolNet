using Examples.WPF.RegisterCollections;
using Examples.WPF.ViewModels;
using MewtocolNet;
using MewtocolNet.ComCassette;
using MewtocolNet.Logging;
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
            IRegister<bool> outputContactReference = null!;
            IRegister<bool> testBoolReference = null!;
            IRegister<Word> wordRefTest = null!;

            //build a new interface
            App.ViewModel.Plc = Mewtocol.Ethernet(viewModel.SelectedIP, parsedInt)
            .WithPoller()
            .WithInterfaceSettings(setting => {

                setting.TryReconnectAttempts = 10;
                setting.TryReconnectDelayMs = 2000;
                setting.SendReceiveTimeoutMs = 1000;
                setting.HeartbeatIntervalMs = 3000;
                setting.MaxDataBlocksPerWrite = 20;
                setting.MaxOptimizationDistance = 10;

            })
            .WithCustomPollLevels(lvl => {

                lvl.SetLevel(2, 3);
                lvl.SetLevel(3, TimeSpan.FromSeconds(5));
                lvl.SetLevel(4, TimeSpan.FromSeconds(10));

            })
            .WithRegisterCollections(collector => {

                App.ViewModel.TestRegCollection = collector.AddCollection<TestRegisterCollection>();

            })
            .WithRegisters(b => {

                b.Bool("X4").Build();
                b.Bool("Y4").Build(out outputContactReference);
                b.Bool("R10A").PollLevel(PollLevel.FirstIteration).Build(out testBoolReference);

                b.Struct<short>("DT1000").Build(out heartbeatSetter);

                //these will be merged into one
                b.Struct<Word>("DT1000").Build(out wordRefTest);
                b.Struct<Word>("DT1000").Build(out wordRefTest);

                b.Struct<ushort>("DT1001").PollLevel(2).Build();
                b.Struct<Word>("DT1002").PollLevel(2).Build();

                b.Struct<int>("DDT1010").PollLevel(2).Build();
                b.Struct<uint>("DDT1012").PollLevel(2).Build();
                b.Struct<DWord>("DDT1014").PollLevel(2).Build();
                b.Struct<float>("DDT1016").PollLevel(2).Build();
                b.Struct<TimeSpan>("DDT1018").PollLevel(2).Build();

                b.Struct<DateAndTime>("DDT1020").PollLevel(2).Build();
                b.Struct<DateAndTime>("DDT1022").PollLevel(2).Build();

                b.String("DT1028", 32).PollLevel(3).Build();
                b.String("DT1046", 5).PollLevel(4).Build();

                b.Struct<Word>("DT1000").AsArray(5).PollLevel(1).Build();

            })
            .WithHeartbeatTask(async (plc) => {

                var randShort = (short)new Random().Next(short.MinValue, short.MaxValue);

                //write direct
                //await heartbeatSetter.WriteAsync(randShort);
                //or by anonymous
                await plc.Register.Struct<short>("DT1000").WriteAsync(randShort);

                //write a register without a reference
                bool randBool = new Random().Next(0, 2) == 1;
                await plc.Register.Bool("Y4").WriteAsync(randBool);

                if (testBoolReference.Value != null)
                    await testBoolReference.WriteAsync(!testBoolReference.Value.Value);

                await plc.Register.Struct<DateAndTime>("DDT1022").WriteAsync(DateAndTime.FromDateTime(DateTime.UtcNow));

            })
            .Build();

            //connect to it
            await App.ViewModel.Plc.ConnectAsync(async () => {

                await App.ViewModel.Plc.RestartProgramAsync();

            });

            await App.ViewModel.Plc.AwaitFirstDataCycleAsync();

            if (App.ViewModel.Plc.IsConnected) {

                App.MainWindow.mainContent.Content = new PlcDataView();

            }

            viewModel.IsConnecting = false;

        }, System.Windows.Threading.DispatcherPriority.Send);

    }

}
