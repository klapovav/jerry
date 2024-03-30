namespace Jerry.SystemQueueModifier;

internal class SystemQueueModifier
{
    public SystemQueueModifierMouse Mouse { get; private set; }
    public SystemQueueModifierKeyboard Keyboard { get; private set; }

    public SystemQueueModifier()
    {
        Keyboard = new(KeyboardInputType.None);
        Mouse = new(MouseInputType.None);
    }

    public bool CanUninstall()
    {
        if (Keyboard is null || Mouse is null)
            return true;
        return Keyboard.AllKeysAreReleased
            && Mouse.AllKeysAreReleased;
    }

    public void Dispose()
    {
        Keyboard?.Dispose();
        Keyboard = null;
        Mouse?.Dispose();
        Mouse = null;
    }

    #region Un(Block) Events & (Un)Subscribe Events

    public void Subscribe(MouseInputType mouseInputType) => Mouse?.Subscribe(mouseInputType);

    public void BlockInputTypes(MouseInputType inputTypes) => Mouse?.BlockInput(inputTypes);

    public void Unsubscribe(MouseInputType mouseInputType) => Mouse?.Unsubscribe(mouseInputType);

    public void KeepInputTypes(MouseInputType inputTypes) => Mouse?.UnblockInput(inputTypes);

    public void Subscribe(KeyboardInputType inputTypes) => Keyboard?.Subscribe(inputTypes);

    public void BlockInputTypes(KeyboardInputType inputTypes) => Keyboard?.BlockInput(inputTypes);

    public void Unsubscribe(KeyboardInputType inputTypes) => Keyboard?.Unsubscribe(inputTypes);

    public void KeepInputTypes(KeyboardInputType inputTypes) => Keyboard?.UnblockInput(inputTypes);

    #endregion Un(Block) Events & (Un)Subscribe Events
}