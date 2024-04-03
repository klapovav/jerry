using System;

namespace Jerry.SystemQueueModifier;

[Flags]
public enum MouseInput
{
    None = 0,
    Move = 1,
    Wheel = 2,
    ButtonUp = 4,
    ButtonDown = 8,
    Button = ButtonUp | ButtonDown,
    All = Move | Wheel | Button
}

[Flags]
public enum KeyboardInput
{
    None = 0,
    KeyUp = 1,
    KeyDown = 2,
    All = KeyUp | KeyDown,
}