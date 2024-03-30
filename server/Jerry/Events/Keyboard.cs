using Jerry.Hook;
using Jerry.Hook.WinApi;
using System.Windows.Forms;

namespace Jerry.Events;

public readonly struct KeyboardHookEvent
{
    private readonly KeyboardHookStruct _hookStruct;

    //System.Windows.Input dle MSDN ~  vkCode 
    //https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
    public readonly Keys Key => (Keys)_hookStruct.vkCode;
    public readonly KeyState KeyState => Flags.HasFlag(KeyFlags.KEY_RELEASED) ? KeyState.KeyUp : KeyState.KeyDown;
    public readonly KeyFlags Flags;
    public readonly uint KeyCode => _hookStruct.vkCode;
    public readonly uint ScanCode => _hookStruct.scanCode;
    public readonly bool Pressed => KeyState == KeyState.KeyDown;
    public readonly uint Time => _hookStruct.time;
    public readonly bool Injected => Flags.HasFlag(KeyFlags.INJECTED);
    public readonly bool SystemKey;
    public readonly nuint ExtraInfo => _hookStruct.dwExtraInfo;

    public KeyboardHookEvent(KeyboardHookStruct ks, int wParam)
    {
        _hookStruct = ks;
        Flags = (KeyFlags)ks.flags;
        SystemKey = wParam switch
        {
            WM.WM_SYSKEYDOWN | WM.WM_SYSKEYUP => true,
            _ => false,
        };
    }

    public MessageSource Source() => (Injected, ExtraInfo) switch
    {
        (false, _) => MessageSource.Hardware,
        (true, Constants.JerryServerID) => MessageSource.JerryServer,
        (true, Constants.JerryClientID) => MessageSource.JerryClient,
        (true, _) => MessageSource.AnotherApp,
    };
}

public enum KeyState
{
    KeyDown = 0,
    KeyUp = 1
}

public enum CreatedBy
{
    Hardware = 0,
    JerryClient = 1,
    JerrySlave = 2,
    AnotherApp = 3
}