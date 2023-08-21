using Examples.WPF.ViewModels;
using MewtocolNet;
using MewtocolNet.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Examples.WPF;

public partial class App : Application {

    public static AppViewModel ViewModel { get; private set; } = null!;

    public static new App Current = null!;

    public static new MainWindow MainWindow = null!;

    public static ObservableCollection<TextBlock> LoggerItems = null!;

    internal static event Action? LogEventProcessed;

    protected override void OnStartup(StartupEventArgs e) {

        ViewModel = new AppViewModel();

        Current = this;
        LoggerItems = new();

        Logger.LogLevel = LogLevel.Verbose;
        Logger.DefaultTargets = LoggerTargets.Trace;

        Logger.OnNewLogMessage((d, l, m) => {

            Application.Current.Dispatcher.BeginInvoke(() => {

                Brush msgColor = null!;

                switch (l) {
                    case LogLevel.Error:
                    msgColor = Brushes.Red;
                    break;
                    case LogLevel.Change:
                    msgColor = Brushes.Blue;
                    break;
                    case LogLevel.Verbose:
                    msgColor = Brushes.Gold;
                    break;
                    case LogLevel.Critical:
                    msgColor = Brushes.Gray;
                    break;
                }

                if (LoggerItems.Count > 1000) LoggerItems.RemoveAt(0);

                var contRun = msgColor == null ? new Run(m) : new Run(m) {
                    Foreground = msgColor,
                };

                LoggerItems.Add(new TextBlock {
                    Inlines = {
                        new Run($"[{d:hh:mm:ss:ff}] ") {
                            Foreground = Brushes.LimeGreen,
                        },
                        contRun
                    }
                });

                LogEventProcessed?.Invoke();

            }, System.Windows.Threading.DispatcherPriority.Background);

        });

    }

}
