using Jerry.Controller;
using NHotkey;
using Serilog;
using System;
using System.Windows.Input;

namespace Jerry.Hotkey;

public sealed class JerryHotkeySettings
{
    private static readonly Lazy<JerryHotkeySettings> lazy = new(() => new JerryHotkeySettings());
    public HotkeyRegistration SwitchMonitor { get; init; }
    public HotkeyRegistration SwitchLoggingLevel { get; init; }
    public JerryKeyGesture SwitchHome { get; }
    public JerryKeyGesture SwitchMouseMode { get; }

    public delegate void OnSwitchMonitorEventHandler();

    public event OnSwitchMonitorEventHandler? OnSwitchMonitorEvent;

    private JerryHotkeySettings()
    {
        var settings = new ConfigurationManager.AppSettings().Load();

        SwitchMonitor = new HotkeyRegistration(HotkeyType.SwitchDestination, settings.SwitchMonitor, OnSwitchMonitor);
        SwitchLoggingLevel = new HotkeyRegistration(HotkeyType.SwitchLoggingLevel, new(HotkeyType.SwitchLoggingLevel, Key.NumPad5, ModifierKeys.Control | ModifierKeys.Alt), OnSwitchLoggingLevel);
        SwitchHome = settings.SwitchHome;
        SwitchMouseMode = settings.SwitchMouseMove;

        Log.Information("Press {a} to toogle between computers - [Global shortcut]",
                    settings.SwitchMonitor.GetDisplayStringForCulture(null)
                    );
        Log.Information("Press {a} to activate server",
                    SwitchHome.GetDisplayStringForCulture(null)
                    );
        Log.Information("Press {0} to turn relative movement on/off",
                    SwitchMouseMode.GetDisplayStringForCulture(null)
                    );
    }

    private void OnSwitchLoggingLevel(object? sender, HotkeyEventArgs e)
    {
        var lvlSwitch = LogController.Instance.LoggingLevelSwitch;
        switch (lvlSwitch.MinimumLevel)
        {
            case Serilog.Events.LogEventLevel.Verbose:
                lvlSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                break;
            case Serilog.Events.LogEventLevel.Debug:
                lvlSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                break;
            case Serilog.Events.LogEventLevel.Information:
                lvlSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
                break;
            case Serilog.Events.LogEventLevel.Warning:
                break;
            case Serilog.Events.LogEventLevel.Error:
                break;
            case Serilog.Events.LogEventLevel.Fatal:
                break;
            default:
                break;
        }
    }

    public static JerryHotkeySettings Instance
    { get { return lazy.Value; } }

    private void OnSwitchMonitor(object? sender, HotkeyEventArgs e) => OnSwitchMonitorEvent?.Invoke();
}