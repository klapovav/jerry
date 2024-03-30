using Jerry.Hook;
using Jerry.Controllable;
using Jerry.Coordinates;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ClipboardData = Common.Clipboard;
using Jerry.Hotkey;

namespace Jerry.ExtendedDesktopManager;

internal class BaseDesktopManager : IExtendedDesktopManager
{
    private ClipboardData SessionClipData { get; set; }
    protected Server LocalComputer { get; }
    private readonly List<IControllableComputer> remoteClients = new();
    private IControllableComputer _active;
    protected IControllableComputer Active
    {
        get { return _active; }
        private set
        {
            if (_active is not null)
            {
                if (_active.OnDeactivate(out ClipboardData clipboard))
                {
                    Log.Debug("Jerry clipboard length: {0}", clipboard.Message.Length);
                    SessionClipData = clipboard;
                }
            }

            if (value.Equals(LocalComputer))
                OnActiveChanged?.Invoke(Strategy.Local);
            else
                OnActiveChanged?.Invoke(Strategy.Remote);
            _active = value;
            _active.OnActivate(SessionClipData);
        }
    }

    public string CurrentVersion => "0.1.xx";
    public IMouseKeyboardEventHandler Subscriber { get; set; } = null;
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
            ||  remoteClients.Where(s => s.ID == client.Info.Guid).Any())
        {
            Log.Error("The new client was rejected due to a non-unique ticket({id}) or guid", client.ID.ID);
            return;
        }
        var remote = new Client(client.Layer, client.ID, client.Info);
        remoteClients.Add(remote);

        Log.Information("New client: \"{GUID}\"[{id}] \t Number of connected clients: {Count}", client.Info.Guid, client.ID.ID, remoteClients.Count);
    }

    public virtual void DisconnectClient(Ticket disc_id)
    {
        

        try
        {
            if (Active.EqualsTicket(disc_id))
            {
                Active = LocalComputer;
            }
            var index = remoteClients.FindIndex(s => s.Ticket == disc_id);
            if (index != -1)
            {
                var name = remoteClients.ElementAt(index).Name;
                remoteClients.RemoveAt(index);
                Log.Information("Client {name}[{id}] disconnected. Number of connected clients: {Count}", name, disc_id.ID, remoteClients.Count);
            }
            else
            {
                Log.Error("VDM.DisconnectClient(\"{id}\") failed", disc_id.ID);
            }
        }
        catch (Exception ex_)
        {
            Log.Error(ex_, "VDM.DisconnectClient() Exception");
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
        if (Subscriber is not null)
        {
            Subscriber?.OnMouseEvent(mouseMove);
        }

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
        var newMon = remoteClients.Where(s => s.EqualsTicket(monitorID)).FirstOrDefault();
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
        var primaryMonitor = Enumerable.Repeat(LocalComputer, 1);
        var newMonitor = primaryMonitor
            .Concat(remoteClients)
            .Concat(primaryMonitor)
            .SkipWhile(s => !s.Equals(Active))
            .Skip(1)
            .FirstOrDefault();

        if (newMonitor is null)
        {
            Log.Error("Could not switch monitors.", Active.Ticket);
            newMonitor = LocalComputer;
        }
        return newMonitor;
    }

    #endregion HotkeyHandler SwitchScreen



    public void SetSubscriber(IMouseKeyboardEventHandler subscriber)
    {
        Subscriber ??= subscriber;
    }

    public void PoisonYourself()
    {
    }

    public Task<bool> TrySendHeartbeat(Ticket id)
    {
        return Task.FromResult(remoteClients.FirstOrDefault(rm => rm.Ticket == id)?.TrySendHeartbeat() ?? false);
    }

    public Task<IEnumerable<Guid>> GetConnectedClients()
    {
        var connectedClients = new List<Guid>();
        var unreachableClients = new List<Ticket>();
        foreach (var client in remoteClients)
        {
            if (client.TrySendHeartbeat())
            {
                connectedClients.Add(client.ID);
            }
            else
            {
                unreachableClients.Add(client.Ticket);
            }
        }
        unreachableClients.ForEach(ticket => DisconnectClient(ticket));
        return Task.FromResult(connectedClients.AsEnumerable());
    }

    public Task<bool> HearbeatAnyClientIsSuccessful()
    {
        var atLeastOneIsHealthy = false;
        var unreachableClients = new List<Ticket>();
        foreach (var client in remoteClients)
        {
            if (client.TrySendHeartbeat())
            {
                atLeastOneIsHealthy = true;
            }
            else
            {
                Log.Debug("Client {TO}[{index}] is considered unreachable/non-responsive.", client.Name, client.Ticket.ID);
                unreachableClients.Add(client.Ticket);
            }
        }
        unreachableClients.ForEach(ticket => DisconnectClient(ticket));
        return Task.FromResult(atLeastOneIsHealthy);
    }
}