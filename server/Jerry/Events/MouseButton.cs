namespace Jerry.Events;

public struct MouseButton
{
    public Button Button { get; private set; }
    public State ButtonPressed { get; private set; }
    public readonly bool IsDown => ButtonPressed == State.Pressed;

    public MouseButton(Button button, State state)
    {
        Button = button;
        ButtonPressed = state;
    }
}

public enum Button
{
    Left = 1,
    Right = 2,
    Middle = 3,
    X1 = 4,
    X2 = 5,
}

public enum State
{
    Released = 0,
    Pressed = 1,
}