using Jerry.Events;
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

public class TrafficController : IDisposable, IInputSubscriber
{
    private readonly IMouseKeyboardEventHandler uiHandler;
    private readonly IGlobalHotkeyHandler hotkeyHandler;
    private readonly SystemQueueModifier.SystemQueueModifier systemQueueModifier;
    private readonly HotkeyEventThrottle eventThrottle;
    private readonly LowLevelKeyboardState lowLevelKeyboardState;
    private Strategy traffic_rules;


    public TrafficController(IExtendedDesktopManager desktopManager)
    {
        this.hotkeyHandler = desktopManager;
        var _initializehotkey = JerryHotkeySettings.Instance;
        _initializehotkey.OnSwitchMonitorEvent += GlobalSystemHotkey_OnSwitchMonitorEvent;
        lowLevelKeyboardState = new LowLevelKeyboardState();
        eventThrottle = new HotkeyEventThrottle();
        systemQueueModifier = new(this);
        TrafficRules = Strategy.Local;
        uiHandler = desktopManager;
    }

    #region -------   Hook Callback -------------------

    public void OnKeyboardEvent(Events.KeyboardHookEvent ke)
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
            }
            // Jerry keyboard shortcut
            if (lowLevelKeyboardState.HotkeyEvent(ke.KeyCode, out JerryKeyGesture gesture))
            {
                if (gesture.Purpose == HotkeyType.SwitchDestination)
                {
                    _ = eventThrottle.TryInvoke(hotkeyHandler);
                }
                else
                {
                    hotkeyHandler.KeyGesture(gesture.Purpose);
                }
            }
        }

        uiHandler.OnKeyboardEvent(ke);
    }

    [Obsolete]
    public void OnMouseEvent(MouseButton ev)
    {
        TryEndTransition();
        uiHandler.OnMouseEvent(ev);
    }
    [Obsolete]
    public void OnMouseEvent(MouseDeltaMove mouseMove)
    {
        TryEndTransition();
        uiHandler.OnMouseEvent(mouseMove);
    }
    [Obsolete]
    public void OnMouseEvent(Events.MouseWheel mouseWheel)
    {
        uiHandler.OnMouseEvent(mouseWheel);
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
                        systemQueueModifier.KeepInput(KeyboardInput.All);
                        systemQueueModifier.KeepInput(MouseInput.All);
                        systemQueueModifier.Unsubscribe(KeyboardInput.All);
                        systemQueueModifier.Unsubscribe(MouseInput.All);
                    });
                    break;

                case Strategy.TransitionToRemote:
                    DispatcherProvider.HookCallbackDispatcher.Invoke(() =>
                    {
                        systemQueueModifier.BlockInput(KeyboardInput.KeyDown);
                        systemQueueModifier.BlockInput(MouseInput.All);
                        systemQueueModifier.KeepInput(MouseInput.ButtonUp);
                        systemQueueModifier.Subscribe(KeyboardInput.All);
                        systemQueueModifier.Subscribe(MouseInput.All);
                    });
                    //cursorPositionChecker.Start();
                    break;

                case Strategy.Remote:
                    DispatcherProvider.HookCallbackDispatcher.Invoke(() =>
                    {
                        systemQueueModifier.BlockInput(KeyboardInput.All);
                        systemQueueModifier.BlockInput(MouseInput.All);
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
                systemQueueModifier.Subscribe(KeyboardInput.All);
                systemQueueModifier.Subscribe(MouseInput.All);
            }
        });
    }

    public void ToLocal() => TrafficRules = Strategy.TransitionToLocal;

    public void ToRemote() => TrafficRules = Strategy.TransitionToRemote;

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