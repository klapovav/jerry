using System;

namespace Jerry.SystemQueueModifier.Emulation.WinApi;

#pragma warning disable 649

internal struct MOUSEINPUT
{
    /// <summary>
    /// Specifies the absolute position of the mouse, or the amount of motion since the last mouse event was generated, depending on the value of the dwFlags member. Absolute data is specified as the x coordinate of the mouse; relative data is specified as the number of pixels moved.
    /// </summary>
    public Int32 X;

    /// <summary>
    /// Specifies the absolute position of the mouse, or the amount of motion since the last mouse event was generated, depending on the value of the dwFlags member. Absolute data is specified as the y coordinate of the mouse; relative data is specified as the number of pixels moved.
    /// </summary>
    public Int32 Y;

    /// <summary>
    /// If dwFlags contains MOUSEEVENTF_WHEEL, then mouseData specifies the amount of wheel movement. A positive value indicates that the wheel was rotated forward, away from the user; a negative value indicates that the wheel was rotated backward, toward the user. One wheel click is defined as WHEEL_DELTA, which is 120.
    /// Windows Vista: If dwFlags contains MOUSEEVENTF_HWHEEL, then dwData specifies the amount of wheel movement. A positive value indicates that the wheel was rotated to the right; a negative value indicates that the wheel was rotated to the left. One wheel click is defined as WHEEL_DELTA, which is 120.
    /// Windows 2000/XP: IfdwFlags does not contain MOUSEEVENTF_WHEEL, MOUSEEVENTF_XDOWN, or MOUSEEVENTF_XUP, then mouseData should be zero.
    /// If dwFlags contains MOUSEEVENTF_XDOWN or MOUSEEVENTF_XUP, then mouseData specifies which X buttons were pressed or released. This value may be any combination of the following flags.
    /// </summary>
    public UInt32 MouseData;

    /// <summary>
    /// A set of bit flags that specify various aspects of mouse motion and button clicks. The bits in this member can be any reasonable combination of the following values.
    /// The bit flags that specify mouse button status are set to indicate changes in status, not ongoing conditions. For example, if the left mouse button is pressed and held down, MOUSEEVENTF_LEFTDOWN is set when the left button is first pressed, but not for subsequent motions. Similarly, MOUSEEVENTF_LEFTUP is set only when the button is first released.
    /// You cannot specify both the MOUSEEVENTF_WHEEL flag and either MOUSEEVENTF_XDOWN or MOUSEEVENTF_XUP flags simultaneously in the dwFlags parameter, because they both require use of the mouseData field.
    /// </summary>
    public UInt32 Flags;

    /// <summary>
    /// Time stamp for the event, in milliseconds. If this parameter is 0, the system will provide its own time stamp.
    /// </summary>
    public UInt32 Time;

    /// <summary>
    /// Specifies an additional value associated with the mouse event. An application calls GetMessageExtraInfo to obtain this extra information.
    /// </summary>
    public IntPtr ExtraInfo;
}

/// <summary>
/// XButton definitions for use in the MouseData property of the <see cref="MOUSEINPUT"/> structure. (See: http://msdn.microsoft.com/en-us/library/ms646273(VS.85).aspx)
/// </summary>
internal enum XButton : uint
{
    /// <summary>
    /// Set if the first X button is pressed or released.
    /// </summary>
    XButton1 = 0x0001,

    /// <summary>
    /// Set if the second X button is pressed or released.
    /// </summary>
    XButton2 = 0x0002,
}

[Flags]
internal enum MouseFlag : uint // UInt32
{
    /// <summary>
    /// Movement occurred.
    /// </summary>
    Move = 0x0001,

    LeftDown = 0x0002,

    LeftUp = 0x0004,

    RightDown = 0x0008,

    RightUp = 0x0010,

    MiddleDown = 0x0020,

    MiddleUp = 0x0040,

    /// <summary>
    /// Windows 2000/XP: Specifies that an X button was pressed.
    /// </summary>
    XDown = 0x0080,

    /// <summary>
    /// Windows 2000/XP: Specifies that an X button was released.
    /// </summary>
    XUp = 0x0100,

    /// <summary>
    /// Windows NT/2000/XP: wheel was moved, if the mouse has a wheel. The amount of movement is specified in mouseData.
    /// </summary>
    VerticalWheel = 0x0800,

    /// <summary>
    /// wheel was moved horizontally, if the mouse has a wheel. The amount of movement is specified in mouseData. Windows 2000/XP:  Not supported.
    /// </summary>
    HorizontalWheel = 0x1000,

    /// <summary>
    /// The WM_MOUSEMOVE messages will not be coalesced. The default behavior is to coalesce WM_MOUSEMOVE messages. Windows XP/2000: This value is not supported.
    /// </summary>
    MouseMoveNoCoalesce = 0x2000,

    /// <summary>
    /// Windows 2000/XP: Maps coordinates to the entire desktop. Must be used with MOUSEEVENTF_ABSOLUTE.
    /// </summary>
    VirtualDesk = 0x4000,

    /// <summary>
    /// Specifies that the dx and dy members contain normalized absolute coordinates. If the flag is not set,
    /// dxand dy contain relative data (the change in position since the last reported position). This flag
    /// can be set, or not set, regardless of what kind of mouse or other pointing device, if any, is connected
    /// to the system. 
    /// </summary>
    Absolute = 0x8000,
}

#pragma warning restore 649
