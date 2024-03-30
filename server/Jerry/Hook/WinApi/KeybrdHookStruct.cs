using System;
using System.Runtime.InteropServices;

namespace Jerry.Hook.WinApi;

[StructLayout(LayoutKind.Sequential)]
public struct KeyboardHookStruct
{
    /// <summary>
    /// A virtual-key code. The code must be a value in the range 1 to 254.
    /// </summary>
    public uint vkCode;

    /// <summary>
    /// A hardware scan code for the key.
    /// </summary>
    public uint scanCode;

    /// <summary>
    /// Specifies the transition state, whether the key is extended key, the event was injected,
    /// </summary>
    [Descriptor(typeof(KeyFlags))]
    public uint flags;

    /// <summary>
    /// The time stamp for this message, equivalent to what GetMessageTime would return for this message.
    /// </summary>
    public uint time;

    /// <summary>
    /// Additional information associated with the message.
    /// </summary>
    public UIntPtr dwExtraInfo;
}

[Flags]
public enum KeyFlags : int
{
    NONE = 0,

    /// <summary>
    /// Specifies whether the key is an extended key, such as a function key or a key on the numeric keypad. The value is 1 if the key is an extended key; otherwise, it is 0.
    /// </summary>
    EXTENDEDKEY = 1 << 0,

    /// <summary>
    /// Specifies whether the event was injected from a process running at lower integrity level. The value is 1 if that is the case; otherwise, it is 0. Note that bit 4 is also set whenever bit 1 is set.
    /// </summary>
    INJECTED_LOWERIL = 1 << 1,

    /// <summary>
    /// Specifies whether the event was injected. The value is 1 if that is the case;
    /// otherwise, it is 0. Note that bit 1 is not necessarily set when bit 4 is set.
    /// </summary>
    INJECTED = 1 << 4,

    /// <summary>
    /// The context code. The value is 1 if the ALT key is pressed; otherwise, it is 0.
    /// </summary>
    ALT_DOWN = 1 << 5,

    /// <summary>
    /// The transition state. The value is 0 if the key is pressed and 1 if it is being released.
    /// </summary>
    KEY_RELEASED = 1 << 7,
}