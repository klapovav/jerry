using Jerry.Controllable;
using Jerry.Hook;
using Jerry.Hotkey;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using ClipboardData = Common.Clipboard;

namespace Jerry.ExtendedDesktopManager;

internal class BaseDesktopManager : IExtendedDesktopManager
{
    private ClipboardData? GlobalClipboard { get; set; }
    protected Server LocalComputer { get; }
    private readonly List<IControllableComputer> remoteClients = new();
    private IControllableComputer _active;
    protected IControllableComputer Active
    {
        get { return _active; }
        private set
        {
            if (_active.OnDeactivate(out var clipboard))
            {
                Log.Debug("Global clipboard length: {0}", clipboard.Message.Length);
                GlobalClipboard = clipboard;
            }
            var newStrategy = value.Equals(LocalComputer) ? Strategy.Local : Strategy.Remote;
            OnActiveChanged.Invoke(newStrategy);
            _active = value;
            _active.OnActivate(GlobalClipboard);
        }
    }

    public string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)?? String.Empty;
    public IMouseKeyboardEventHandler? Subscriber { get; set; } = null;
    public virtual Mode Mode => Mode.Basic;
    public Action<Strategy> OnActiveChanged { get; }
    public BaseDesktopManager(Action<Strategy> onActiveChanged)
    {
        LocalComputer = new(new Ticket(0));
        _active = LocalComputer;
        OnActiveChanged = onActiveChanged;
    }

    public virtual void RegisterClient(ConnectedClient client)
    {
        if (remoteClients.Where(s => s.Ticket == client.ID).Any()
            || remoteClients.Where(s => s.ID == client.Info.Guid).Any())
        {
            Log.Error("The new client was rejected due to a non-unique ticket({id}) or guid", client.ID.ID);
            return;
        }
        var remote = new Client(client.Layer, client.ID, client.Info);
        remoteClients.Add(remote);

        Log.Information("New client: \"{GUID}\"[{id}] \t Number of connected clients: {Count}", client.Info.Guid, client.ID.ID, remoteClients.Count);
    }

    public virtual void DisconnectClient(Ticket idToDisconnect)
    {
        try
        {
            if (Active.EqualsTicket(idToDisconnect))
            {
                Active = LocalComputer;
            }
            var index = remoteClients.FindIndex(s => s.Ticket == idToDisconnect);
            if (index == -1)
            {
                Log.Error("VDM.DisconnectClient(\"{id}\") failed", idToDisconnect.ID);
                return;
            }
            var clientToDisconnect = remoteClients.ElementAt(index);
            remoteClients.RemoveAt(index);
            Log.Information("Client {name}[{id}] disconnected. Number of connected clients: {Count}", clientToDisconnect.Name, idToDisconnect.ID, remoteClients.Count);

        }
        catch (Exception ex)
        {
            Log.Error(ex, "VDM.DisconnectClient() Exception");
        }
    }

    public void ReleaseModifiers(ModifierKeys modifiers)
    {
        Active.ReleaseModifiers(modifiers);
    }

    public void OnKeyboardEvent(Events.KeyboardHookEvent key)
    {
        Subscriber?.OnKeyboardEvent(key);
        Active.OnKeyEvent(key);
    }

    public void OnMouseEvent(Events.MouseButton ev)
    {
        Subscriber?.OnMouseEvent(ev);
        Active.OnMouseClick(ev);
    }

    public void OnMouseEvent(Events.MouseWheel ev)
    {
        Subscriber?.OnMouseEvent(ev);
        Active.OnMouseWheel(ev);
    }

    public virtual void OnMouseEvent(Events.MouseDeltaMove mouseMove)
    {
        Subscriber?.OnMouseEvent(mouseMove);

        if (Active.Equals(LocalComputer))
        {
            LocalComputer.OnMouseMove(mouseMove.X, mouseMove.Y);
        }
        else
        {
            Active.OnMouseMove(mouseMove.DX, mouseMove.DY);
        }
    }

    #region HotkeyHandler


    public void KeyGesture(HotkeyType type)
    {
        Log.Debug("Pressed hotkey {a} ", type);
        switch (type)
        {
            case HotkeyType.SwitchToServer:
                Switch(LocalComputer);
                break;

            case HotkeyType.SwitchDestination:
                Switch(GetNextScreen());
                break;

            case HotkeyType.SwitchMouseMove:
                Active.ToogleMouseMode();
                break;

            default:
                break;
        }
    }

    protected void Switch(IControllableComputer newMonitor)
    {
        Log.Information("{TO}[{index}]", newMonitor.Name, newMonitor.Ticket.ID);

        if (newMonitor.Equals(Active))
        {
            OnActiveChanged?.Invoke(Strategy.Local);
            return;
        }

        Active = newMonitor;
    }

    protected void SwitchTo(Ticket monitorID)
    {
        var newMon = remoteClients
            .Append(LocalComputer)
            .Where(s => s.EqualsTicket(monitorID))
            .FirstOrDefault();
        if (newMon == default(IControllableComputer))
        {
            Log.Information("Switch to client number {id} failed.", monitorID);
            return;
        }
        Switch(newMon);
    }

    private IControllableComputer GetNextScreen()
    {
        if (!remoteClients.Any())
            return LocalComputer;
        var server = Enumerable.Repeat(LocalComputer, 1);
        var newMonitor = server
            .Concat(remoteClients)
            .Concat(server)
            .SkipWhile(s => !s.Equals(Active))
            .Skip(1)
            .FirstOrDefault();

        if (newMonitor is null)
        {
            Log.Error("Could not switch computers", Active.Ticket);
            newMonitor = LocalComputer;
        }
        return newMonitor;
    }

    #endregion HotkeyHandler

    public void SetSubscriber(IMouseKeyboardEventHandler subscriber) => Subscriber ??= subscriber;

    public void PoisonYourself() { }

    public Task<bool> TrySendHeartbeat(Ticket id)
    {
        return Task.FromResult(remoteClients.FirstOrDefault(rm => rm.Ticket == id)?.TrySendHeartbeat() ?? false);
    }

    public Task<IEnumerable<Guid>> GetConnectedClients()
    {
        UpdateRemoteClients();
        return Task.FromResult(remoteClients.Select(cl => cl.ID));
    }

    public Task<bool> HearbeatAnyClientIsSuccessful()
    {
        this.UpdateRemoteClients();
        var atLeastOneIsHealthy = remoteClients.Count > 0;
        return Task.FromResult(atLeastOneIsHealthy);
    }
    private void UpdateRemoteClients()
    {
        var unreachableClients = remoteClients
            .Where(cl => !cl.TrySendHeartbeat())
            .ToList();
        unreachableClients.ForEach(client =>
        {
            Log.Debug("Client {TO}[{index}] is considered unreachable/non-responsive.", client.Name, client.Ticket.ID);
            DisconnectClient(client.Ticket); // ~ remoteClient.Remove
        });
    }
}