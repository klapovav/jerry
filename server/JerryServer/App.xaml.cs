using Hardcodet.Wpf.TaskbarNotification;
using Jerry;
using Jerry.Controller;
using Serilog;
using Shared.WinApi;
using System;
using System.Windows;

namespace JerryServer;

public partial class App : Application
{
    private TaskbarIcon trayIcon;
    public TaskbarIcon TrayIconInstance => trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Logger initialization
        _ = LogController.Instance;
        DispatcherProvider.Init(Current.Dispatcher);
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        SetCurrentThreadExtra(Jerry.Constants.JerryServerID);
        trayIcon = (TaskbarIcon)FindResource("NotifyIcon");
    }

    private void SetCurrentThreadExtra(UInt16 id)
    {
        var _prevExtraInfo = User32.SetMessageExtraInfo(id);
        Log.Debug("Message identifier: {0}", User32.GetMessageExtraInfo());
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Error($"Current domain exception: {e.ExceptionObject}");
        trayIcon.Visibility = Visibility.Collapsed;
        trayIcon.Dispose();
        Log.CloseAndFlush();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information($"Exit code: {e.ApplicationExitCode}");
        trayIcon.Visibility = Visibility.Collapsed;
        if (trayIcon.DataContext is TrayIconVM vm)
        {
            vm.Dispose();
        }
        trayIcon.Dispose();

        base.OnExit(e);
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        if (e.ApplicationExitCode != 0)
        {
            Log.Error($"Exit code: {e.ApplicationExitCode}");
        }
        if (trayIcon.IsDisposed)
        {
            trayIcon = null;
        }
        else
        {
            trayIcon.Dispose();
        }

        Log.CloseAndFlush();
    }
}