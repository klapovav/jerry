using Serilog;
using System;
using System.Threading.Tasks;

namespace Jerry.Connection;

internal class ClientHealthChecker
{
    private DateTime earliestStopTime;
    private Task<bool>? previousHeartbeatResult;
    private readonly IClientManager clientManager;
    private readonly PeriodicWorker worker;
    public readonly int CHECK_INTERVAL = 1000;

    public ClientHealthChecker(IClientManager vdm)
    {
        worker = new PeriodicWorker(IntervalElapsedCallback);
        clientManager = vdm;
    }

    public void Start()
    {
        KeepRunning(TimeSpan.FromSeconds(5));
        previousHeartbeatResult = null;
        worker.Start(CHECK_INTERVAL);
    }

    public void KeepRunning(TimeSpan interval)
    {
        earliestStopTime = DateTime.Now + interval;
    }

    private void IntervalElapsedCallback()
    {
        if (previousHeartbeatResult is not null)
        {
            var res = previousHeartbeatResult;
            if (res.IsFaulted || res.IsCanceled)
            {
                Log.Error("Send heartbeat failed due to an exception or timeout");
                return;
            }
            if (!res.Result && DateTime.Now > earliestStopTime)
            {
                worker.Stop();
                return;
            }
        }

        previousHeartbeatResult = clientManager.HearbeatAnyClientIsSuccessful();
    }

    public void Dispose()
    {
        worker?.Stop();
        worker?.Dispose();
    }
}