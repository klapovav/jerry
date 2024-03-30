using Hardcodet.Wpf.TaskbarNotification;
using Jerry;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
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
        base.OnStartup(e);
        LoggerInitialization();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        SetCurrentThreadExtra(Jerry.Constants.JerryServerID);
        DispatcherProvider.Init(Current.Dispatcher);
        trayIcon = (TaskbarIcon)FindResource("JTrayIcon");
    }

    private static void LoggerInitialization()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}")
            .WriteTo.File("log/", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}")
            .CreateLogger();

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