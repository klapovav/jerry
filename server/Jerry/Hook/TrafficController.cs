using Jerry.Hotkey;
using Jerry.SystemQueueModifier;
using Serilog;
using System;

namespace Jerry.Hook;

public enum Strategy : int
{
    Local = 0,
    Remote = 1,
    TransitionToRemote = 2,
    TransitionToLocal = 3
}

public class TrafficController : IDisposable
{
    private readonly IMouseKeyboardEventHandler uiHandler;
    private readonly IGlobalHotkeyHandler hotkeyHandler;
    private readonly SystemQueueModifier.SystemQueueModifier systemQueueModifier;
    private readonly HotkeyEventThrottle eventThrottle;
    private readonly LowLevelKeyboardState lowLevelKeyboardState;
    private Strategy traffic_rules;
    private readonly (bool, bool) LOCAL = (true, false);
    private readonly (bool, bool) REMOTE = (false, true);
    private readonly (bool, bool)[] KeyDownStrategy = new (bool, bool)[4];
    private readonly (bool, bool)[] KeyUpStrategy = new (bool, bool)[4];
    private readonly (bool, bool)[] DefaultStrategy = new (bool, bool)[4];

    public TrafficController(IExtendedDesktopManager desktopManager)
    {
        this.hotkeyHandler = desktopManager;
        var _initializehotkey = JerryHotkeySettings.Instance;
        _initializehotkey.OnSwitchMonitorEvent += GlobalSystemHotkey_OnSwitchMonitorEvent;
        lowLevelKeyboardState = new LowLevelKeyboardState();
        eventThrottle = new HotkeyEventThrottle();

        systemQueueModifier = new();
        systemQueueModifier.Mouse.OnMouseMove += SystemQueueModifier_OnMouseMove;
        systemQueueModifier.Mouse.OnMouseButton += SystemQueueModifier_OnMouseButton;
        systemQueueModifier.Mouse.OnMouseWheel += SystemQueueModifier_OnMouseWheel;

        systemQueueModifier.Keyboard.OnKeyboardEvent += SystemQueueModifier_OnKeyboardEvent;

        DefaultStrategy[(int)Strategy.Local] = LOCAL;
        DefaultStrategy[(int)Strategy.Remote] = REMOTE;
        DefaultStrategy[(int)Strategy.TransitionToRemote] = REMOTE;
        DefaultStrategy[(int)Strategy.TransitionToLocal] = LOCAL;

        KeyDownStrategy[(int)Strategy.Local] = LOCAL;
        KeyDownStrategy[(int)Strategy.Remote] = REMOTE;
        KeyDownStrategy[(int)Strategy.TransitionToRemote] = REMOTE;
        KeyDownStrategy[(int)Strategy.TransitionToLocal] = LOCAL;

        KeyUpStrategy[(int)Strategy.Local] = LOCAL;
        KeyUpStrategy[(int)Strategy.Remote] = REMOTE;
        KeyUpStrategy[(int)Strategy.TransitionToRemote] = (true, true);
        KeyUpStrategy[(int)Strategy.TransitionToLocal] = LOCAL;

        TrafficRules = Strategy.Local;
        uiHandler = desktopManager;
    }

    #region -------   Handle Hook Callback -------------------

    private FilterResult SystemQueueModifier_OnKeyboardEvent(Events.KeyboardHookEvent ke)
    {
        TryEndTransition();
        lowLevelKeyboardState.KeyEvent(ke.KeyCode, ke.Pressed);

        if (ke.Pressed)
        {
            //System keyboard shortcut
            if (lowLevelKeyboardState.SystemGesturePressed(ke.KeyCode, out KeyGesture sysgesture))
            {
                lowLevelKeyboardState.ReleaseModifiers();
                uiHandler.ReleaseModifiers(sysgesture.Modifiers);
                return FilterResult.Discard;
            }
            // Jerry keyboard shortcut
            if (lowLevelKeyboardState.HotkeyEvent(ke.KeyCode, out JerryKeyGesture gesture))
            {
                if (gesture.Purpose == HotkeyType.SwitchDestination)
                {
                    _ = eventThrottle.TryInvoke(hotkeyHandler);
                    return FilterResult.Discard;
                }
                else
                {
                    hotkeyHandler.KeyGesture(gesture.Purpose);
                    return FilterResult.Discard;
                }
            }
        }

        var (passEvent, sendToRemote) = GetKeyStrategy(ke.Pressed);
        if (sendToRemote)
        {
            uiHandler.OnKeyboardEvent(ke);
        }

        return passEvent ?
            FilterResult.Keep :
            FilterResult.Discard;
    }

    private FilterResult SystemQueueModifier_OnMouseWheel(Events.MouseWheel mouseWheel)
    {
        var (passEvent, sendToRemote) = GetStrategy(mouseWheel);
        if (sendToRemote)
            uiHandler.OnMouseEvent(mouseWheel);
        return passEvent ?
            FilterResult.Keep :
            FilterResult.Discard;
    }

    private FilterResult SystemQueueModifier_OnMouseButton(Events.MouseButton mouseButton)
    {
        TryEndTransition();
        var (passEvent, sendToRemote) = GetStrategy(mouseButton);

        if (sendToRemote)
            uiHandler.OnMouseEvent(mouseButton);
        return passEvent ?
            FilterResult.Keep :
            FilterResult.Discard;
    }

    private FilterResult SystemQueueModifier_OnMouseMove(Events.MouseDeltaMove deltaMove)
    {
        TryEndTransition();

        var (passEvent, sendToRemote) = DefaultStrategy[(int)TrafficRules];

        if (sendToRemote)
            uiHandler.OnMouseEvent(deltaMove);

        return passEvent ?
            FilterResult.Keep :
            FilterResult.Discard;
    }

    #endregion -------   Handle Hook Callback -------------------

    public Strategy TrafficRules
    {
        get
        {
            return traffic_rules;
        }
        private set
        {
            var newStrategy = value;
            switch (newStrategy)
            {
                case Strategy.Local:
                case Strategy.TransitionToLocal:
                    DispatcherProvider.HookCallbackDispatcher.Invoke(() =>
                    {
                        systemQueueModifier.KeepInputTypes(KeyboardInputType.All);
                        systemQueueModifier.KeepInputTypes(MouseInputType.All);
                        systemQueueModifier.Unsubscribe(KeyboardInputType.All);
                        systemQueueModifier.Unsubscribe(MouseInputType.All);
                    });
                    break;

                case Strategy.TransitionToRemote:
                    DispatcherProvider.HookCallbackDispatcher.Invoke(() =>
                    {
                        systemQueueModifier.BlockInputTypes(KeyboardInputType.KeyDown);
                        systemQueueModifier.BlockInputTypes(MouseInputType.All);
                        systemQueueModifier.Subscribe(KeyboardInputType.All);
                        systemQueueModifier.Subscribe(MouseInputType.All);
                    });
                    //cursorPositionChecker.Start();
                    break;

                case Strategy.Remote:
                    DispatcherProvider.HookCallbackDispatcher.Invoke(() =>
                    {
                        systemQueueModifier.BlockInputTypes(KeyboardInputType.All);
                        systemQueueModifier.BlockInputTypes(MouseInputType.All);
                    });
                    break;
            }
            traffic_rules = value;
            Log.Debug("Traffic controller strategy: {Strategy}", value);
        }
    }

    private void GlobalSystemHotkey_OnSwitchMonitorEvent()
    {
        DispatcherProvider.HookCallbackDispatcher.Invoke(() =>
        {
            if (eventThrottle.TryInvoke(hotkeyHandler))
            {
                systemQueueModifier.Subscribe(KeyboardInputType.All);
                systemQueueModifier.Subscribe(MouseInputType.All);
            }
        });
    }

    public void ToLocal() => TrafficRules = Strategy.TransitionToLocal;

    public void ToRemote() => TrafficRules = Strategy.TransitionToRemote;

    private (bool, bool) GetStrategy(Events.MouseButton btn)
    {
        return GetKeyStrategy(btn.IsDown);
    }

    private (bool, bool) GetStrategy(Events.MouseWheel _)
    {
        return StatelessEventsStrategy();
    }

    private (bool, bool) StatelessEventsStrategy()
    {
        return (TrafficRules) switch
        {
            Strategy.Local => (true, false),
            Strategy.Remote => (false, true),
            Strategy.TransitionToRemote => (false, true),
            Strategy.TransitionToLocal => (true, false),
            _ => throw new NotImplementedException(),
        };
    }

    private (bool, bool) GetKeyStrategy(bool down)
    {
        return (TrafficRules) switch
        {
            Strategy.Local => LOCAL,
            Strategy.Remote => REMOTE,
            Strategy.TransitionToRemote => down ? REMOTE : (true, true),
            Strategy.TransitionToLocal => LOCAL,
            _ => throw new NotImplementedException(),
        };
    }

    private void TryEndTransition()
    {
        switch (TrafficRules)
        {
            case Strategy.Local:
            case Strategy.Remote:
                return;

            case Strategy.TransitionToLocal:
                if (systemQueueModifier.CanUninstall())
                    TrafficRules = Strategy.Local;
                break;

            case Strategy.TransitionToRemote:
                if (systemQueueModifier.CanUninstall())
                    TrafficRules = Strategy.Remote;
                break;
        }
    }

    public void Dispose() => systemQueueModifier.Dispose();
}