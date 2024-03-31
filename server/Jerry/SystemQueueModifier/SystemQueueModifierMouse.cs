using Jerry.Events;
using Jerry.Hook;
using Jerry.Hook.WinApi;
using Serilog;

namespace Jerry.SystemQueueModifier;

internal sealed class SystemQueueModifierMouse : SystemQueueModifierBase<MouseInputType>
{
    public event OnMouseMoveEventHandler OnMouseMove;

    public event OnMouseWheelEventHandler OnMouseWheel;

    public event OnMouseButtonEventHandler OnMouseButton;

    public delegate FilterResult OnMouseMoveEventHandler(Events.MouseDeltaMove deltaMove);

    public delegate FilterResult OnMouseWheelEventHandler(Events.MouseWheel mouseWheel);

    public delegate FilterResult OnMouseButtonEventHandler(Events.MouseButton mouseButton);

    private KeyboardSyncSupervisor buttonSyncSupervisor;
    private CursorPosSyncSupervisor cursorSyncSupervisor;

    public override bool AllKeysAreReleased => buttonSyncSupervisor.AllKeysAreReleased();
    protected override IHook hook => MouseHook;
    private MouseHook MouseHook;

    public SystemQueueModifierMouse(MouseInputType noneEvent) : base(noneEvent)
    {
        var mouseHook = new MouseHook();
        mouseHook.OnMouseButton += MouseHook_OnMouseButton;
        mouseHook.OnMouseMove += MouseHook_OnMouseMove;
        mouseHook.OnMouseWheel += MouseHook_OnMouseWheel;
        MouseHook = mouseHook;
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
        var controllerOSResponse = OnMouseWheel?.Invoke(mouseWheel) ?? FilterResult.Keep;

        var stopPropagation = BlockedInputTypes.HasFlag(MouseInputType.Wheel);

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
        var controllerOSResponse = OnMouseMove?.Invoke(ev) ?? FilterResult.Keep;
        var stopPropagation = BlockedInputTypes.HasFlag(MouseInputType.Move);
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

    private FilterResult MouseHook_OnMouseButton(Events.MouseButton mouseButton)
    {
        var controllerOSResponse = OnMouseButton?.Invoke(mouseButton) ?? FilterResult.Keep;
        var stopPropagation = mouseButton.IsDown
            ? BlockedInputTypes.HasFlag(MouseInputType.ButtonDown)
            : BlockedInputTypes.HasFlag(MouseInputType.ButtonUp);

        return controllerOSResponse;
    }

    public void Dispose()
    {
        MouseHook.OnMouseButton -= MouseHook_OnMouseButton;
        MouseHook.OnMouseMove -= MouseHook_OnMouseMove;
        MouseHook.OnMouseWheel -= MouseHook_OnMouseWheel;
        MouseHook.Dispose();
        MouseHook = null;
    }
}