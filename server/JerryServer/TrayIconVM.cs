using Jerry;
using Jerry.ConfigurationManager;
using Jerry.Connection;
using Jerry.Controller;
using Jerry.ExtendedDesktopManager;
using Jerry.Hook;
using Serilog;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace JerryServer;

/// <summary>
/// Provides bindable properties and commands for the NotifyIcon.
/// The view model is assigned to the NotifyIcon in XAML. Alternatively, the startup routing
/// in App.xaml.cs could have created this view model, and assigned it to the NotifyIcon.
/// </summary>
public class TrayIconVM : INotifyPropertyChanged
{
    public string ServerInfoAddress { get; init; }
    public string ServerInfoPassword { get; init; }

    private Settings settings;
    private TcpServer rawTcp;
    private TrafficController trafficController;
    private ExtendedDesktopManager desktopManager;

    private Mode jerryMode;


    public Mode JerryMode
    {
        get { return jerryMode; }
        set
        {
            jerryMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ChangeModeTitle));

            ClearDependencies();

            desktopManager = new ExtendedDesktopManager(value, OnActiveChanged);
            trafficController = new TrafficController(desktopManager);
            rawTcp = new TcpServer(desktopManager, settings);
            rawTcp.StartListening();
        }
    }

    private void OnActiveChanged(Strategy newState)
    {
        switch (newState)
        {
            case Strategy.Local:
                trafficController.ToLocal();
                break;

            case Strategy.Remote:
                trafficController.ToRemote();
                break;

            default: break;
        }
    }

    public TrayIconVM()
    {
        settings = new AppSettings().Load();
        //JerryMode = Settings.EnableMouseGesture ? Mode.Extended : Mode.Basic ;
        JerryMode = Mode.Basic;
        var endPoint = IPAddressProvider.GetEndPoint();
        ServerInfoAddress = endPoint.ToString();
        ServerInfoPassword = String.Format($"Password: \"{settings.Password}\"");
    }

    public string ShowHideLogHeader => LogController.Instance.ConsoleWindow.IsVisible
        ? "Hide log window"
        : "Show log window";
    public string ChangeModeTitle => JerryMode == Mode.Basic
                ? "Enable mouse gesture"
                : "Disable mouse gesture";

    public string StartStopListeningHeader => rawTcp.IsRunning
        ? "Stop"
        : "Start";

    public string StartStopListeningTitle => rawTcp.IsRunning
        ? "Block incoming connections.\nIt does not affect connections already established"
        : "Allow new connections to be established";

    public string DisconnectClientsTitle => rawTcp.IsRunning
        ? "Reconnect clients"
        : "Disconnect clients";


    public ICommand ShowHideLogCommand => new DelegateCommand
    {
        CanExecuteFunc = () => true,
        CommandAction = () =>
        {
            LogController.Instance.ConsoleWindow.ChangeVisibility();
            OnPropertyChanged(nameof(ShowHideLogHeader));
        }
    };
    public ICommand DisconnectClientsCommand => new DelegateCommand
    {
        CanExecuteFunc = () => true,
        CommandAction = () =>
        {
            rawTcp.DisconnectAll();
        }
    };

    public ICommand SwitchModeCommand => new DelegateCommand
    {
        CanExecuteFunc = () => true,
        CommandAction = () =>
        {
            JerryMode = (JerryMode) switch
            {
                Mode.Basic => Mode.Layout,
                Mode.Layout => Mode.Basic,
                _ => throw new NotImplementedException(),
            };
        }
    };

    public ICommand StartStopListeningCommand => new DelegateCommand
    {
        CanExecuteFunc = () => true,
        CommandAction = () =>
        {
            if (rawTcp.IsRunning)
                rawTcp.StopListening();
            else
                rawTcp.StartListening();
            OnPropertyChanged(nameof(StartStopListeningTitle));
            OnPropertyChanged(nameof(StartStopListeningHeader));
            OnPropertyChanged(nameof(DisconnectClientsTitle));
        }
    };

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    public ICommand ExitApplicationCommand
    {
        get
        {
            return new DelegateCommand
            {
                CommandAction = () =>
                {
                    Log.Debug("Exit button pressed");
                    ClearDependencies();
                    Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
                }
            };
        }
    }

    private void ClearDependencies()
    {
        trafficController?.Dispose();
        rawTcp?.Dispose();
        desktopManager?.PoisonYourself();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void Dispose()
    {
        ClearDependencies();
        Log.Debug("TrayIconViewModel disposed");
    }
}