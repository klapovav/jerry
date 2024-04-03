using Jerry.Hook;
using Jerry.Hotkey;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Jerry.ExtendedDesktopManager;

public enum Mode
{
    Basic = 0,
    Layout = 1,
}

public sealed class ExtendedDesktopManager : IExtendedDesktopManager
{
    /// <summary>
    /// multiple-producer,single-consumer
    /// </summary>
    private readonly IExtendedDesktopManager Implementation;

    private readonly BlockingCollection<Task> _jobs;
    private readonly Thread consumerThread;

    public ExtendedDesktopManager(Mode mode, Action<Strategy> onActiveChanged)
    {
        Mode = mode;
        Implementation = mode switch
        {
            Mode.Layout => new LayoutDesktopManager(onActiveChanged),
            Mode.Basic => new BaseDesktopManager(onActiveChanged),
            _ => new BaseDesktopManager(onActiveChanged),
        };

        //Log.Information("Version: {Version}", this.CurrentVersion);
        Log.Information("Mode: {Mode}", mode);

        _jobs = new BlockingCollection<Task>(300);
        consumerThread = new Thread(Consumer)
        {
            IsBackground = true
        };
        consumerThread.Start();
    }

    private void Consumer()
    {
        try
        {
            while (!_jobs.IsCompleted)
            {
                Task t = _jobs.Take();
                try
                {
                    t.RunSynchronously();
                    if (t.IsFaulted)
                    {
                        throw t.Exception;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("[EDM] Exception {0}", e.Message);
                    Log.Error("[EDM] Inner exception {0}", e.InnerException);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("[EDM] Blocking collection disposed:  {0}", e.Message);
        }
        Log.Debug("[EDM] Message consumer completed.");
    }

    public void KeyGesture(HotkeyType type)
    {
        try
        {
            var t = new Task(() => Implementation.KeyGesture(type));
            _jobs.TryAdd(t, Timeout.Infinite);
        }
        catch (Exception)
        { }
    }

    public void RegisterClient(ConnectedClient client)
    {
        try
        {
            var t = new Task(() => Implementation.RegisterClient(client));
            _jobs.TryAdd(t, Timeout.Infinite);
        }
        catch (Exception) { }
    }

    public void DisconnectClient(Ticket id)
    {
        try
        {
            var t = new Task(() => Implementation.DisconnectClient(id));
            _jobs.TryAdd(t, Timeout.Infinite);
        }
        catch (Exception) { }
    }

    public Task<IEnumerable<Guid>> GetConnectedClients()
    {
        try
        {
            var tsc = new TaskCompletionSource<IEnumerable<Guid>>();
            var t = new Task(async () =>
            {
                tsc.SetResult(await Implementation.GetConnectedClients());
            });
            _jobs.TryAdd(t, Timeout.Infinite);
            return tsc.Task;
        }
        catch (Exception) { return Task.FromResult(Enumerable.Empty<Guid>()); }
    }

    public Task<bool> HearbeatAnyClientIsSuccessful()
    {
        try
        {
            var tsc = new TaskCompletionSource<bool>();
            var t = new Task(async () =>
            {
                tsc.SetResult(await Implementation.HearbeatAnyClientIsSuccessful());
            });
            _jobs.TryAdd(t, Timeout.Infinite);
            return tsc.Task;
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> TrySendHeartbeat(Ticket id)
    {
        try
        {
            var tsc = new TaskCompletionSource<bool>();
            var t = new Task(async () =>
            {
                tsc.SetResult(await Implementation.TrySendHeartbeat(id));
            });
            _jobs.TryAdd(t, 1000);
            return tsc.Task;
        }
        catch (Exception) { return Task.FromResult(false); }
    }

    public void ReleaseModifiers(ModifierKeys modifiers)
    {
        try
        {
            var t = new Task(() => Implementation.ReleaseModifiers(modifiers));
            _jobs.TryAdd(t, Timeout.Infinite);
        }
        catch (Exception) { }
    }

    public void OnKeyboardEvent(Events.KeyboardHookEvent key)
    {
        try
        {
            var t = new Task(() => Implementation.OnKeyboardEvent(key));
            _jobs.TryAdd(t, Timeout.Infinite);
        }
        catch (Exception) { }
    }

    public void OnMouseEvent(Events.MouseDeltaMove mouseMove)
    {
        try
        {
            var t = new Task(() => Implementation.OnMouseEvent(mouseMove));
            _jobs.TryAdd(t, 300);
        }
        catch (Exception) { }
    }

    public void OnMouseEvent(Events.MouseButton ev)
    {
        try
        {
            var t = new Task(() => Implementation.OnMouseEvent(ev));
            _jobs.TryAdd(t, Timeout.Infinite);
        }
        catch (Exception) { }
    }

    public void OnMouseEvent(Events.MouseWheel ev)
    {
        try
        {
            var t = new Task(() => Implementation.OnMouseEvent(ev));
            _jobs.TryAdd(t, Timeout.Infinite);
        }
        catch (Exception) { }
    }

    public void PoisonYourself()
    {
        try
        {
            var t = new Task(() => Implementation.PoisonYourself());
            _jobs.TryAdd(t, Timeout.Infinite);
            _jobs.CompleteAdding();
        }
        catch (Exception) { }
    }

    public string CurrentVersion => Implementation.CurrentVersion;

    public Mode Mode { get; init; }
}