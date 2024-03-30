using Jerry.Hook;
using Jerry.Hook.WinApi;
using Jerry.Coordinates;
using System.Drawing;

namespace Jerry.Events;

public readonly struct MouseDeltaMove : IVector, ICoordinate
{
    private readonly MouseHookStruct mouseStruct;
    public readonly MessageSource Source;

    public MouseDeltaMove(NativePoint lastMousePosition, MouseHookStruct currentMouseStruct)
    {
        mouseStruct =currentMouseStruct;
        var current = currentMouseStruct;
        DX = current.pt.x - lastMousePosition.x;
        DY = current.pt.y - lastMousePosition.y;
        X = current.pt.x;
        Y = current.pt.y;

        Source = ((MouseFlags)current.flags, current.dwExtraInfo) switch
        {
            (MouseFlags.NOT_INJECTED, _) => MessageSource.Hardware,
            (MouseFlags.INJECTED, Constants.JerryServerID) => MessageSource.JerryServer,
            (MouseFlags.INJECTED, Constants.JerryClientID) => MessageSource.JerryClient,
            //(MouseFlags.INJECTED | MouseFlags.LOWER_IL_INJECTED, _) => MessageSource.AnotherAppLowerLevel,
            (MouseFlags.INJECTED, _) => MessageSource.AnotherApp,
            (_, _) => MessageSource.AnotherApp, 
        };
    }

    public readonly Point IntoPoint => new(X, Y);
    public int DX { get; }
    public int X { get; }
    public int DY { get; }
    public int Y { get; }

    public readonly bool IsInjected => (MouseFlags)mouseStruct.flags != MouseFlags.NOT_INJECTED;
}