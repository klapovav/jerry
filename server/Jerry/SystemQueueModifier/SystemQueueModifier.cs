namespace Jerry.SystemQueueModifier;
#nullable disable

internal class SystemQueueModifier
{
    public SystemQueueModifierMouse Mouse { get; private set; }
    public SystemQueueModifierKeyboard Keyboard { get; private set; }

    public SystemQueueModifier(IInputSubscriber subscriber)
    {
        Keyboard = new(KeyboardInput.None, subscriber);
        Mouse = new(MouseInput.None, subscriber);
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

    public void Subscribe(MouseInput mouseInputType) => Mouse?.Subscribe(mouseInputType);

    public void BlockInput(MouseInput inputTypes) => Mouse?.BlockInput(inputTypes);

    public void Unsubscribe(MouseInput mouseInputType) => Mouse?.Unsubscribe(mouseInputType);

    public void KeepInput(MouseInput inputTypes) => Mouse?.UnblockInput(inputTypes);

    public void Subscribe(KeyboardInput inputTypes) => Keyboard?.Subscribe(inputTypes);

    public void BlockInput(KeyboardInput inputTypes) => Keyboard?.BlockInput(inputTypes);

    public void Unsubscribe(KeyboardInput inputTypes) => Keyboard?.Unsubscribe(inputTypes);

    public void KeepInput(KeyboardInput inputTypes) => Keyboard?.UnblockInput(inputTypes);

    #endregion Un(Block) Events & (Un)Subscribe Events
}