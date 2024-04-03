using Jerry.Events;
using Jerry.Hook;
using Jerry.Hook.WinApi;
using Serilog;

namespace Jerry.SystemQueueModifier;

internal sealed class SystemQueueModifierMouse : SystemQueueModifierBase<MouseInput>
{
    private KeyboardSyncSupervisor buttonSyncSupervisor;
    private CursorPosSyncSupervisor cursorSyncSupervisor;
    public override bool AllKeysAreReleased => buttonSyncSupervisor.AllKeysAreReleased();
    protected override IHook hook => MouseHook;
    private MouseHook MouseHook;
    private IInputSubscriber subscriber;

    public SystemQueueModifierMouse(MouseInput noneEvent, IInputSubscriber subscriber) : base(noneEvent)
    {
        var mouseHook = new MouseHook();
        mouseHook.OnMouseButton += MouseHook_OnMouseButton;
        mouseHook.OnMouseMove += MouseHook_OnMouseMove;
        mouseHook.OnMouseWheel += MouseHook_OnMouseWheel;
        MouseHook = mouseHook;
        this.subscriber = subscriber;
    }

    protected override void InstallHook()
    {
        if (MouseHook is null || MouseHook.IsInstalled)
            return;
        MouseHook.Install();

        buttonSyncSupervisor = new(true);
        cursorSyncSupervisor = new();
    }

    private FilterResult MouseHook_OnMouseWheel(Events.MouseWheel mouseWheel)
    {
        if (SubscribedInput.HasFlag(MouseInput.Wheel))
        {
            subscriber.OnMouseEvent(mouseWheel);
        }
        var stopPropagation = BlockedInputTypes.HasFlag(MouseInput.Wheel);

        return stopPropagation
            ? FilterResult.Discard
            : FilterResult.Keep;
    }

    private FilterResult MouseHook_OnMouseMove(MouseHookStruct mouseHookStruct)
    {
        return cursorSyncSupervisor.TryGetCursorPosition(out var fixedCursorPosition)
            ? OnMouseMoveRelative(new MouseDeltaMove(fixedCursorPosition, mouseHookStruct))
            : FilterResult.Discard;
    }

    private FilterResult OnMouseMoveRelative(MouseDeltaMove ev)
    {
        if (ev.IsInjected && ev.Source == MessageSource.AnotherApp)
        {
            Log.Debug("[X] SQM.MouseMove discarded. Event injected by another app");
            return FilterResult.Discard;
        }
        if (SubscribedInput.HasFlag(MouseInput.Move))
        {
            subscriber.OnMouseEvent(ev);
        }
        var stopPropagation = BlockedInputTypes.HasFlag(MouseInput.Move);
        if (!stopPropagation)
        {
            cursorSyncSupervisor.ExpectMsgInSystemQueue();
        }

        var filterResult = stopPropagation
        ? FilterResult.Discard
        : FilterResult.Keep;

        Log.Verbose("[_] MouseMoveEvent position: ({x}x{y}) | delta ({dx}x{dy}) | result {resp}", ev.X, ev.Y, ev.DX, ev.DY, filterResult);
        return filterResult;
    }

    private FilterResult MouseHook_OnMouseButton(MouseButton mouseButton)
    {
        if (SubscribedInput.HasFlag(MouseInput.Button))
        {
            subscriber.OnMouseEvent(mouseButton);
        }
        var stopPropagation = mouseButton.IsDown
            ? BlockedInputTypes.HasFlag(MouseInput.ButtonDown)
            : BlockedInputTypes.HasFlag(MouseInput.ButtonUp);

        return stopPropagation
        ? FilterResult.Discard
        : FilterResult.Keep;
    }

    public void Dispose()
    {
        MouseHook.OnMouseButton -= MouseHook_OnMouseButton;
        MouseHook.OnMouseMove -= MouseHook_OnMouseMove;
        MouseHook.OnMouseWheel -= MouseHook_OnMouseWheel;
        MouseHook.Dispose();
        MouseHook = null;
        subscriber = null;
    }
}