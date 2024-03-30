using NHotkey;
using Serilog;
using System;

namespace Jerry.Hotkey;

public sealed class JerryHotkeySettings
{
    private static readonly Lazy<JerryHotkeySettings> lazy = new(() => new JerryHotkeySettings());
    public HotkeyRegistration SwitchMonitor { get; init; }
    public JerryKeyGesture SwitchHome { get; }
    public JerryKeyGesture SwitchMouseMode { get; }

    public delegate void OnSwitchMonitorEventHandler();

    public event OnSwitchMonitorEventHandler OnSwitchMonitorEvent;

    private JerryHotkeySettings()
    {
        var settings = new ConfigurationManager.AppSettings().Load();

        SwitchMonitor = new HotkeyRegistration(HotkeyType.SwitchDestination, settings.SwitchMonitor, OnSwitchMonitor);
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

    public static JerryHotkeySettings Instance
    { get { return lazy.Value; } }

    private void OnSwitchMonitor(object sender, HotkeyEventArgs e) => OnSwitchMonitorEvent?.Invoke();
}