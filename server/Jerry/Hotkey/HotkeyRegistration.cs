using NHotkey;
using System;
using System.Windows.Forms;

namespace Jerry.Hotkey;

public class HotkeyRegistration
{
    public HotkeyType Purpose { get; }
    private string Name { get; } 
    private EventHandler<HotkeyEventArgs> Handler { get; }
    private JerryKeyGesture _keyGesture = default!;

    public HotkeyRegistration(HotkeyType hotkey, JerryKeyGesture keys, EventHandler<HotkeyEventArgs> handler)
    {
        Name = hotkey.ToString();
        Purpose = hotkey;
        Handler = handler;
        KeyGesture = keys;
        NHotkey.Wpf.HotkeyManager.Current.AddOrReplace(Name, keys, false, Handler);
    }

    public JerryKeyGesture KeyGesture
    {
        get { return _keyGesture; }
        set
        {
            var prevG = _keyGesture;

            try
            {
                NHotkey.Wpf.HotkeyManager.Current.Remove(Name);
                NHotkey.Wpf.HotkeyManager.Current.AddOrReplace(Name, value, false, Handler);
                _keyGesture = value;
            }
            catch
            {
                MessageBox.Show(string.Format("The global shortcut `{0}` is already registered by another process. Set another shortcut in the configuration and restart the program.",
                    value.GetDisplayStringForCulture(null)

                ));
            }
        }
    }
}