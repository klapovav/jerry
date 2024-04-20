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
    private bool isRunning = false;

    public ClientHealthChecker(IClientManager vdm)
    {
        worker = new PeriodicWorker(IntervalElapsedCallback);
        clientManager = vdm;
    }

    public void Start()
    {
        if (isRunning)
        {
            return;
        }
        KeepRunning(TimeSpan.FromSeconds(5));
        isRunning = true;
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
                isRunning = false;
                worker.Stop();
                return;
            }
        }

        previousHeartbeatResult = clientManager.HearbeatAnyClientIsSuccessfulAsync();
    }

    public void Dispose()
    {
        worker?.Stop();
        worker?.Dispose();
    }
}