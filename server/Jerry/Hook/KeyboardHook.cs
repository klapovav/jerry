using Jerry.Events;
using Jerry.Hook.WinApi;
using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Jerry.Hook;
#nullable disable

public sealed class KeyboardHook : BaseHook, IHook, IDisposable
{
    public event OnKeyboardEventHandler OnKeyboardEvent;

    public delegate FilterResult OnKeyboardEventHandler(KeyboardHookEvent keyboardEvent);

    public KeyboardHook() : base(HookType.KeyboardHook)
    { }

    protected override IntPtr OnHookCall(int nCode, IntPtr wParam, IntPtr lParam)
    {
        using var a = new PerformanceStopwatch(dataCollector);

        if (nCode < 0 || OnKeyboardEvent is null)
        {
            return CallNextHook(nCode, wParam, lParam);
        }

        return LowLevelKeyboardProc(wParam, lParam) switch
        {
            FilterResult.Discard => (IntPtr)WinApi.Constants.CONSUME_INPUT,
            FilterResult.Keep => CallNextHook(nCode, wParam, lParam),
            _ => throw new NotImplementedException(),
        };
    }

    private FilterResult LowLevelKeyboardProc(IntPtr wParam, IntPtr lParam)
    {
        KeyboardHookStruct keyboardStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

        var kbEvent = new KeyboardHookEvent(keyboardStruct, (int)wParam);

        LogStruct(kbEvent);
        //            if (kbEvent.Source() == MessageSource.JerryClient)
        //            {
        //#if DEBUGLOOPBACK
        //            if (kbEvent.Injected)
        //                return FilterResult.Keep;
        //#endif
        //                return FilterResult.Discard;
        //            }

        if (kbEvent.Key == Keys.NumLock)
        {
            return FilterResult.Keep;
        }

        var consumeEvent = OnKeyboardEvent?.Invoke(kbEvent) ?? FilterResult.Keep;
        return consumeEvent;
    }

    private void LogStruct(KeyboardHookEvent kb)
    {
        var keyDescription = kb.Key.ToString().PadLeft(10);// + new string(' ', 10);
        var maxKeyLength = 10;
        if (keyDescription.Length > maxKeyLength)
            keyDescription = keyDescription.Substring(0, maxKeyLength);
        //Log.Verbose("{@type} filter function:  {m_type,11} {behav,15} {x,4}x{y,4} {inj,11} {s}", HookType.MouseHook, (MouseMessage)wParam, response, st.pt.x, st.pt.y, (MouseFlags)st.flags, stackalloc.);

        Log.Verbose("Keyboard | SysKey: {msg,5} | scan: (0x){scan:X4} | virtual: {keydes} = (0x){keycode:X2} | flags {last} | dw: {ex} time: {t}", kb.SystemKey, kb.ScanCode, keyDescription, kb.KeyCode, FlagsDescrition(kb.Flags), kb.ExtraInfo, kb.Time);
    }

    private string FlagsDescrition(KeyFlags flags)
    {
        string ext = flags.HasFlag(KeyFlags.EXTENDEDKEY) ? "EXTENDED" : "";
        string alt = flags.HasFlag(KeyFlags.ALT_DOWN) ? "ALT" : "";
        string injected = flags.HasFlag(KeyFlags.INJECTED) ? "FAKE" : "";
        string released = flags.HasFlag(KeyFlags.KEY_RELEASED) ? "UP" : "";
        return String.Format("| {3,4} | {2, 2} | {1,8} | {0, 3} ", alt, ext, released, injected);
    }
}