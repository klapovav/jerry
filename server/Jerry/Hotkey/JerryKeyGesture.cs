using System.Windows.Input;

namespace Jerry.Hotkey;
public class JerryKeyGesture : KeyGesture
{
    internal HotkeyType Purpose { get; init; }
    public JerryKeyGesture(HotkeyType purpose, Key key, ModifierKeys modifiers) : base(key, modifiers)
    {
        Purpose = purpose;

    }
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