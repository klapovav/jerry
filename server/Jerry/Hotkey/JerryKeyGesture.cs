using System;
using System.Windows.Input;

namespace Jerry.Hotkey;

public class JerryKeyGesture : KeyGesture
{
    internal HotkeyType Purpose { get; init; }

    public JerryKeyGesture(HotkeyType purpose, Key key, ModifierKeys modifiers) : base(key, modifiers)
    {
        Purpose = purpose;
    }
    public static JerryKeyGesture Default(HotkeyType type) => type switch
    {
        HotkeyType.SwitchDestination => new JerryKeyGesture(type, Key.N, ModifierKeys.Control | ModifierKeys.Windows),
        HotkeyType.SwitchToServer => new JerryKeyGesture(type, Key.H, ModifierKeys.Control | ModifierKeys.Windows),
        HotkeyType.SwitchMouseMove => new JerryKeyGesture(type, Key.F1, ModifierKeys.Control | ModifierKeys.Alt),
        _ => throw new NotImplementedException(),
    };
}

public class KeyGesture : System.Windows.Input.KeyGesture
{
    internal uint VirtualKeyCode { get; init; }

    //public KeyGesture(Key key) : base (key)
    //{
    //    VirtualKeyCode = (uint)KeyInterop.VirtualKeyFromKey(key);
    //}
    public KeyGesture(Key key, ModifierKeys modifiers) : base(key, modifiers)
    {
        VirtualKeyCode = (uint)KeyInterop.VirtualKeyFromKey(key);
    }
}