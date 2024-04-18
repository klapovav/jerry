using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jerry;

internal class PeriodicWorker : IDisposable
{
    private CancellationTokenSource? cancellationToken;
    private Task? longRunningTask;
    private readonly Action job;
    private int intervalMillis;
    public bool IsRunning => longRunningTask?.Status == TaskStatus.Running;

    public PeriodicWorker(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        job = callback;
    }
    public void Start(int interval)
    {
        intervalMillis = interval;
        cancellationToken = new CancellationTokenSource();
        longRunningTask = Task.Factory.StartNew(
            () => OnTick(cancellationToken.Token),
            cancellationToken.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }
    public void Stop()
    {
        cancellationToken?.Cancel();
        longRunningTask?.Wait();
        longRunningTask = null;
        cancellationToken = null;
    }
    public void Dispose()
    {
        Stop();
        cancellationToken?.Dispose();
    }

    private void OnTick(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            job.Invoke();
            Thread.Sleep(intervalMillis);
        }
    }
}
