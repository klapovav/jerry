using Jerry.Hook.SysGlobalState;
using Serilog;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Forms;
using ModifierKeys = System.Windows.Input.ModifierKeys;

namespace Jerry.Hotkey;

public class LowLevelKeyboardState
{
    private readonly IEnumerable<JerryKeyGesture> JerryHotkeys;
    private readonly IEnumerable<KeyGesture> SystemILHotkeys;
    private ModifierKeys pressedModifiers;

    public LowLevelKeyboardState()
    {
        JerryHotkeys = new List<JerryKeyGesture>
        {
            JerryHotkeySettings.Instance.SwitchMonitor.KeyGesture,
            JerryHotkeySettings.Instance.SwitchHome,
            JerryHotkeySettings.Instance.SwitchMouseMode,
        };
        SystemILHotkeys = new List<KeyGesture>
        {
            new(System.Windows.Input.Key.Delete, ModifierKeys.Control | ModifierKeys.Alt),
            new(System.Windows.Input.Key.L, ModifierKeys.Windows)
        };
    }

    public void ReleaseModifiers() => pressedModifiers = ModifierKeys.None;

    public void KeyEvent(uint keyCode, bool pressed)
    {
        var modifier = ToModifier((Keys)keyCode);
        if (modifier == ModifierKeys.None)
            return;
        if (pressed)
            Press(modifier);
        else Release(modifier);
    }

    private void Press(ModifierKeys modifier) => pressedModifiers |= modifier;

    private void Release(ModifierKeys modifier) => pressedModifiers &= ~modifier;

    public bool HotkeyEventOccurs(uint keyCode, [MaybeNullWhen(false)] out JerryKeyGesture pressedGesture)
    {
        pressedGesture = null;
        foreach (var hotkey in JerryHotkeys)
        {
            if (keyCode != hotkey.VirtualKeyCode)
                continue;
            if (pressedModifiers == hotkey.Modifiers)
            {
                //Log.Verbose("[LL] Pressed hotkey {a} (home)", SwitchHomeGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture));
                //Log.Verbose("[LL] Pressed hotkey {a} (absolute/relative move)", SwitchHomeGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture));
                Log.Verbose("[LL] Pressed hotkey {a} ", hotkey.GetDisplayStringForCulture(CultureInfo.CurrentCulture));
                pressedGesture = hotkey;
                return true;
            }
            if (hotkey.Purpose == HotkeyType.SwitchDestination && Keyboard.GetPressedModifiers() == hotkey.Modifiers)
            {
                Log.Verbose("[LL] Pressed hotkey {a} ", hotkey.GetDisplayStringForCulture(CultureInfo.CurrentCulture));
                pressedGesture = hotkey;
                return true;
            }
        }
        return false;
    }

    public bool SystemGesturePressed(uint keyCode, [MaybeNullWhen(false)] out KeyGesture gesture)
    {
        gesture = null;
        foreach (var hotkey in SystemILHotkeys)
        {
            if (keyCode != hotkey.VirtualKeyCode)
                continue;
            if (pressedModifiers == hotkey.Modifiers)
            {
                gesture = hotkey;
                return true;
            }
        }
        return false;
    }

    private static ModifierKeys ToModifier(Keys key) => key switch
    {
        >= Keys.LShiftKey and <= Keys.RMenu => GetModifierFromRange(key),
        Keys.LWin or Keys.RWin => ModifierKeys.Windows,
        _ => ModifierKeys.None,
    };

    private static ModifierKeys GetModifierFromRange(Keys key) => key switch
    {
        Keys.LShiftKey or Keys.RShiftKey => ModifierKeys.Shift,
        Keys.LControlKey or Keys.RControlKey => ModifierKeys.Control,
        Keys.LMenu or Keys.RMenu => ModifierKeys.Alt,
        _ => ModifierKeys.None,
    };
}