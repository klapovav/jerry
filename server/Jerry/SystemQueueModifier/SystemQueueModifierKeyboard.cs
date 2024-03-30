using Jerry.Events;
using Jerry.Hook;

namespace Jerry.SystemQueueModifier;
internal sealed class SystemQueueModifierKeyboard : SystemQueueModifierBase<KeyboardInputType>
{
    private KeyboardHook KeyboardHook;
    private KeyboardSyncSupervisor keyboardState;
    public event OnKeyboardEventHandler OnKeyboardEvent;
    public delegate FilterResult OnKeyboardEventHandler(KeyboardHookEvent keyboardHookEvent);

    public SystemQueueModifierKeyboard(KeyboardInputType noneEvent) : base(noneEvent)
    {
        KeyboardHook = new KeyboardHook();
        KeyboardHook.OnKeyboardEvent += LL_KeyboardHook_OnKeyboardEvent;
    }

    private FilterResult LL_KeyboardHook_OnKeyboardEvent(KeyboardHookEvent keyboardEvent)
    {
        var controllerOSResponse = OnKeyboardEvent?.Invoke(keyboardEvent) ?? FilterResult.Keep;

        var stopPropagation = keyboardEvent.Pressed
            ? BlockedInputTypes.HasFlag(KeyboardInputType.KeyDown)
            : BlockedInputTypes.HasFlag(KeyboardInputType.KeyUp);
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
    }
}