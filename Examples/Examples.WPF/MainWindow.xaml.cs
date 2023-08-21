using Examples.WPF.ViewModels;
using Examples.WPF.Views;
using MewtocolNet;
using MewtocolNet.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Examples.WPF;

public partial class MainWindow : Window {

    public ObservableCollection<TextBlock> LoggerItems => App.LoggerItems;

    public AppViewModel AppViewModel => App.ViewModel;

    public MainWindow() {

        InitializeComponent();

        this.DataContext = this;
        App.MainWindow = this;

        mainContent.Content = new ConnectView();

        loggerList.PreviewMouseWheel += (s, e) => {

            autoScrollBtn.IsChecked = false;

        };

        App.LogEventProcessed += () => {

            Application.Current.Dispatcher.BeginInvoke(() => {

                if (autoScrollBtn?.IsChecked != null && autoScrollBtn.IsChecked.Value)
                    loggerList.ScrollIntoView(App.LoggerItems.Last());

            }, System.Windows.Threading.DispatcherPriority.Send);

        };

    }

}
