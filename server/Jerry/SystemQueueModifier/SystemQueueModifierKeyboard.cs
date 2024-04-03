using Jerry.Events;
using Jerry.Hook;

namespace Jerry.SystemQueueModifier;

internal sealed class SystemQueueModifierKeyboard : SystemQueueModifierBase<KeyboardInput>
{
    private KeyboardHook KeyboardHook;
    private KeyboardSyncSupervisor keyboardState;
    private IInputSubscriber subscriber;
    public SystemQueueModifierKeyboard(KeyboardInput noneEvent, IInputSubscriber subscriber) : base(noneEvent)
    {
        KeyboardHook = new KeyboardHook();
        KeyboardHook.OnKeyboardEvent += LL_KeyboardHook_OnKeyboardEvent;
        this.subscriber = subscriber;
    }

    private FilterResult LL_KeyboardHook_OnKeyboardEvent(KeyboardHookEvent keyboardEvent)
    {
        subscriber.OnKeyboardEvent(keyboardEvent);

        var stopPropagation = keyboardEvent.Pressed
            ? BlockedInputTypes.HasFlag(KeyboardInput.KeyDown)
            : BlockedInputTypes.HasFlag(KeyboardInput.KeyUp);
        //AssertEqualResponse(controllerOSResponse, stopPropagation,
        //"Key " + keyboardEvent.Key.ToString() + keyboardEvent.KeyState.ToString());

        return stopPropagation
        ? FilterResult.Discard
            : FilterResult.Keep;
    }

    public override bool AllKeysAreReleased => keyboardState.AllKeysAreReleased();

    protected override IHook hook => KeyboardHook;

    protected override void InstallHook()
    {
        if (KeyboardHook is null || KeyboardHook.IsInstalled)
            return;
        KeyboardHook.Install();
        keyboardState = new KeyboardSyncSupervisor();
    }

    public void Dispose()
    {
        KeyboardHook.OnKeyboardEvent -= LL_KeyboardHook_OnKeyboardEvent;
        KeyboardHook.Dispose();
        KeyboardHook = null;
        subscriber = null;
    }
}