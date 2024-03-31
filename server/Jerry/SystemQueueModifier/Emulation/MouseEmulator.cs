using Jerry.SystemQueueModifier.Emulation.WinApi;
using System;
using System.Runtime.InteropServices;

namespace Jerry.SystemQueueModifier.Emulation;

public enum MouseButton
{
    Left,
    Right,
    Middle,
    X1,
    X2,
}

public class MouseEmulator
{
    public MouseEmulator()
    {
    }

    /// <summary>
    /// Simulates mouse movement by the specified distance measured as a delta from the current mouse location.
    /// </summary>
    /// <param name="mickeyX">The distance to move the mouse horizontally.</param>
    /// <param name="mickeyY">The distance to move the mouse vertically.</param>
    public bool MouseMoveRaw(int mickeyX, int mickeyY)
    {
        var movement = new INPUT { Type = (UInt32)InputType.Mouse };
        movement.Data.Mouse.Flags = (UInt32)MouseFlag.Move;
        movement.Data.Mouse.X = mickeyX;
        movement.Data.Mouse.Y = mickeyY;
        movement.Data.Mouse.ExtraInfo = WinApi.NativeMethods.GetMessageExtraInfo();
        return Simulate(movement);
    }

    /// <summary>
    /// Simulates mouse movement to the specified location on the primary display device.
    /// </summary>
    /// <param name="absoluteX">The destination's absolute X-coordinate on the primary display device where 0 is the extreme left hand side of the display device and 65535 is the extreme right hand side of the display device.</param>
    /// <param name="absoluteY">The destination's absolute Y-coordinate on the primary display device where 0 is the top of the display device and 65535 is the bottom of the display device.</param>
    public bool MouseMovePrimary(double absoluteX, double absoluteY, bool jump)
    {
        var absX = (int)Math.Truncate(absoluteX);
        var absY = (int)Math.Truncate(absoluteY);
        var moveType = jump ? MouseFlag.Move | MouseFlag.Absolute | MouseFlag.MouseMoveNoCoalesce : MouseFlag.Move | MouseFlag.Absolute;
        var movement = new INPUT { Type = (UInt32)InputType.Mouse };
        movement.Data.Mouse.Flags = (UInt32)(moveType);
        movement.Data.Mouse.X = absX;
        movement.Data.Mouse.Y = absY;
        movement.Data.Mouse.ExtraInfo = WinApi.NativeMethods.GetMessageExtraInfo();
        return Simulate(movement);
    }

    public bool MouseDown(MouseButton button) => Mouse(button, false);

    public bool MouseUp(MouseButton button) => Mouse(button, true);

    private bool Mouse(MouseButton button, bool up)
    {
        var (flags, data) = up ? ToMouseButtonUpFlag(button) : ToMouseButtonDownFlag(button);

        var mouseInput = new INPUT { Type = (UInt32)InputType.Mouse };
        mouseInput.Data.Mouse.Flags = (UInt32)flags;
        mouseInput.Data.Mouse.MouseData = (UInt32)data;
        mouseInput.Data.Mouse.ExtraInfo = WinApi.NativeMethods.GetMessageExtraInfo();
        return Simulate(mouseInput);
    }

    private static bool Simulate(INPUT input)
    {
        INPUT[] inputs = new INPUT[] { input };
        return WinApi.NativeMethods.SendInput((UInt32)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) == 1;
    }

    private static bool Simulate(INPUT[] inputs)
    {
        return WinApi.NativeMethods.SendInput((UInt32)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) == 1;
    }

    private static (MouseFlag, uint) ToMouseButtonDownFlag(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => (MouseFlag.LeftDown, 0),
            MouseButton.Right => (MouseFlag.RightDown, 0),
            MouseButton.Middle => (MouseFlag.MiddleDown, 0),
            MouseButton.X1 => (MouseFlag.XDown, 0x0001),
            MouseButton.X2 => (MouseFlag.XDown, 0x0002),
            _ => throw new NotImplementedException(),
        };
    }

    private static (MouseFlag, uint) ToMouseButtonUpFlag(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => (MouseFlag.LeftUp, 0),
            MouseButton.Right => (MouseFlag.RightUp, 0),
            MouseButton.Middle => (MouseFlag.MiddleUp, 0),
            MouseButton.X1 => (MouseFlag.XUp, 0x0001),
            MouseButton.X2 => (MouseFlag.XUp, 0x0002),
            _ => throw new NotImplementedException(),
        };
    }
}