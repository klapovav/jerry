using Jerry.Events;
using Jerry.Hook.WinApi;
using Serilog;
using System;
using System.Runtime.InteropServices;

namespace Jerry.Hook;
public enum MouseEvent
{
    Move,
    Wheel,
    Button,
    Unknown
}

public sealed class MouseHook : BaseHook
{
    public event OnMouseMoveEventHandler OnMouseMove;
    public event OnMouseWheelEventHandler OnMouseWheel;
    public event OnMouseButtonEventHandler OnMouseButton;
    public delegate FilterResult OnMouseMoveEventHandler(MouseHookStruct mouseHookStruct);
    public delegate FilterResult OnMouseWheelEventHandler(Events.MouseWheel mouseWheel);
    public delegate FilterResult OnMouseButtonEventHandler(Events.MouseButton mouseButton);

    public MouseHook() : base(HookType.MouseHook)
    { }

    protected override IntPtr OnHookCall(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0) // if nCode != WinApi.Constants.HC_ACTION
            return CallNextHook(nCode, wParam, lParam);

        using var _a = new PerformanceStopwatch(dataCollector);

        return LowLevelMouseProc(wParam, lParam) switch
        {
            FilterResult.Discard => (IntPtr)WinApi.Constants.CONSUME_INPUT,
            FilterResult.Keep => CallNextHook(nCode, wParam, lParam),
            _ => throw new NotImplementedException(),
        };
    }

    private void OnUnknownEvent(MouseEvent type, MouseHookStruct hookStruct)
    {
        Log.Error("[BUG] Low-level mouse hook message {type}: {str}", type, hookStruct.ToString());
    }

    /// <summary>   
    /// </summary>
    /// <param name="nCode"></param>
    /// <param name="wParam">The identifier of the mouse message.</param>
    /// <param name="mouseStruct"></param>
    /// <returns></returns>
    private FilterResult LowLevelMouseProc(IntPtr wParam, IntPtr lParam)
    {
        MouseHookStruct mouseStruct = (MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookStruct));
        var now = Environment.TickCount;
        var delay = now - mouseStruct.time;


//            if (GetMessageSource(mouseStruct) == MessageSource.JerryClient)
//            {

//#if DEBUGLOOPBACK //Keyboard 
//            return wParam == WM.WM_MOUSEMOVE ? FilterResult.Discard : FilterResult.Keep;
//#endif
//                return FilterResult.Discard;
//            }
        var response = FilterResult.Keep;
        switch (wParam)
        {
            case WM.WM_MOUSEMOVE:
                response = OnMouseMove?.Invoke(mouseStruct) ?? FilterResult.Keep;
                break;

            case WM.WM_MOUSEWHEEL:
            case WM.WM_MOUSEHWHEEL:
                if (Mapper.TryIntoWheel((int)wParam, mouseStruct, out Events.MouseWheel result))
                    response = OnMouseWheel?.Invoke(result) ?? FilterResult.Keep;
                else
                    OnUnknownEvent(MouseEvent.Wheel, mouseStruct);
                break;

            default:
                if (Mapper.TryIntoButton((int)wParam, mouseStruct, out Events.MouseButton button))
                    response = OnMouseButton?.Invoke(button) ?? FilterResult.Keep;
                else
                    OnUnknownEvent(MouseEvent.Button, mouseStruct);
                break;
        }
        var st = mouseStruct;
        
        Log.Verbose("{@type} filter function: {m_type,8} {behav,8} {x,4}x{y,4} {inj,11} {s}", HookType.MouseHook, (MouseMessage)wParam, response, st.pt.x, st.pt.y, (MouseFlags)st.flags, GetMessageSource(mouseStruct));

        return response;
    }

    private MessageSource GetMessageSource(MouseHookStruct mouseStruct)
    {
        var flags = (MouseFlags)mouseStruct.flags;

        return (flags, mouseStruct.dwExtraInfo) switch
        {
            
            (MouseFlags.NOT_INJECTED, _) => MessageSource.Hardware,
            (MouseFlags.INJECTED, Constants.JerryServerID) => MessageSource.JerryServer,
            (MouseFlags.INJECTED, Constants.JerryClientID) => MessageSource.JerryClient, 
            //(MouseFlags.INJECTED | MouseFlags.LOWER_IL_INJECTED, _) => MessageSource.AnotherAppLowerLevel,
            (MouseFlags.INJECTED, _) => MessageSource.AnotherApp,
            (_, _) => MessageSource.Hardware,
        };
    }

    private static class Mapper
    {
        public static bool TryIntoWheel(int wParam, MouseHookStruct ms, out Events.MouseWheel result)
        {
            var amount = (short)NativeMethods.HIWORD(ms.mouseData);
            result = (wParam, amount) switch
            {
                // A positive value indicates that the wheel was rotated forward (i.e. away from the user).
                // A negative value indicates that the wheel was rotated backward (i.e. toward the user).
                (WM.WM_MOUSEWHEEL, < 0) => new(Direction.Down, amount), //settings 4 lines: 120, -120
                (WM.WM_MOUSEWHEEL, > 0) => new(Direction.Up, amount),   
                //away from the user;
                (WM.WM_MOUSEHWHEEL, < 0) => new(Direction.Left, amount),
                (WM.WM_MOUSEHWHEEL, > 0) => new(Direction.Right, amount), //settings 1 char: 30, -30
                _ => new(),
            };
            return result.Amount != 0;
        }

        public static bool TryIntoButton(int wParam, MouseHookStruct ms, out MouseButton result)
        {
            try
            {
                var button = IntoButton(wParam, ms);
                result = new MouseButton(button.Item1, button.Item2);
                return true;
            }
            catch (ArgumentException)
            {
                result = new MouseButton();
                return false;
            }
        }

        /// <exception cref="ArgumentException"></exception>
        private static (Button, State) IntoButton(int wParam, MouseHookStruct ms) =>
            (wParam, ms.mouseData) switch
            {
                (WM.WM_LBUTTONDOWN, _) => (Button.Left, State.Pressed),
                (WM.WM_LBUTTONUP, _) => (Button.Left, State.Released),
                (WM.WM_RBUTTONDOWN, _) => (Button.Right, State.Pressed),
                (WM.WM_RBUTTONUP, _) => (Button.Right, State.Released),
                (WM.WM_MBUTTONDOWN, _) => (Button.Middle, State.Pressed),
                (WM.WM_MBUTTONUP, _) => (Button.Middle, State.Released),
                (WM.WM_XBUTTONDOWN, 0x10000) => (Button.X1, State.Pressed),
                (WM.WM_XBUTTONUP, 0x10000) => (Button.X1, State.Released),
                (WM.WM_XBUTTONDOWN, 0x20000) => (Button.X2, State.Pressed),
                (WM.WM_XBUTTONUP, 0x20000) => (Button.X2, State.Released),
                _ => throw new ArgumentException(String.Format("Unknown mouse button {0}, mouseData: {1}", wParam, ms.mouseData)),
            };
    }
}