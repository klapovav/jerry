using Jerry.Hook.WinApi;
using System.Runtime.InteropServices;

namespace Jerry.Hook.SysGlobalState;

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;

    public static implicit operator NativePoint(POINT point)
    {
        return new NativePoint { x = point.X, y = point.Y };
    }
}

public static class Cursor
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    public static NativePoint GetCursorPosition()
    {
        POINT lpPoint;
        GetCursorPos(out lpPoint);

        return new NativePoint { x = lpPoint.X, y = lpPoint.Y };
    }
}