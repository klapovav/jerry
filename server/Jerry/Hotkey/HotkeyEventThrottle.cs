using System;

namespace Jerry.Hotkey;

/// <summary>
/// The keystroke events for switching to another machine are merged from two different
/// event sources and filtered by this class to avoid switching twice in a short period.
/// </summary>

public class HotkeyEventThrottle
{
    private DateTime lastUnthrottledEventTime;
    private static readonly int THROTTLETIME = 150;

    public IGlobalHotkeyHandler VirtualDesk { get; }

    public HotkeyEventThrottle()
    {
        lastUnthrottledEventTime = DateTime.MinValue;
    }

    public bool TryInvoke(IGlobalHotkeyHandler hotkeyHandler)
    {
        var now = DateTime.Now;
        var elapsed = now - lastUnthrottledEventTime;
        if (elapsed.TotalMilliseconds < THROTTLETIME)
        {
            return false;
        }
        lastUnthrottledEventTime = now;
        hotkeyHandler.KeyGesture(HotkeyType.SwitchDestination);
        return true;
    }
}