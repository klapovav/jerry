using Jerry.ExtendedDesktopManager;
using Jerry.Hotkey;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Jerry;

[ImplementedBy(typeof(ExtendedDesktopManager.ExtendedDesktopManager))]
[ImplementedBy(typeof(BaseDesktopManager))]
[ImplementedBy(typeof(LayoutDesktopManager))]
public interface IExtendedDesktopManager : IGlobalHotkeyHandler, IClientManager, IMouseKeyboardEventHandler, ISingleConsumer
{
    public Mode Mode { get; }
    public string CurrentVersion { get; }
}

public interface IGlobalHotkeyHandler
{
    void KeyGesture(HotkeyType type);
}

public interface IClientManager
{
    void RegisterClient(ConnectedClient proxy);

    void DisconnectClient(Ticket id);

    Task<bool> TrySendHeartbeatAsync(Ticket id);

    /// <summary>
    /// Send a heartbeat to all clients and update the list
    /// of connected clients based on successful heartbeat responses.
    /// </summary>
    /// <returns>The list of responsive clients</returns>
    Task<IEnumerable<Guid>> GetConnectedClientsAsync();

    /// <summary>
    /// Sends heartbeat to all connected clients to maintain the connection.
    /// </summary>
    /// <returns>Returns true if at least one client is connected</returns>
    Task<bool> HearbeatAnyClientIsSuccessfulAsync();
}
public interface IInputSubscriber
{
    void OnMouseEvent(Events.MouseButton ev);

    void OnMouseEvent(Events.MouseDeltaMove mouseMove);

    void OnMouseEvent(Events.MouseWheel ev);
    void OnKeyboardEvent(Events.KeyboardHookEvent key);
}
public interface IMouseKeyboardEventHandler : IInputSubscriber
{
    void ReleaseModifiers(ModifierKeys modifiers);
}

public interface ISingleConsumer
{
    public void PoisonYourself();
}