using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jerry;

internal interface IPeriodicWorker : IDisposable
{
    internal void Start(IntervalElapsedCallback callback, int interval);
    internal void Stop();
    bool IsRunning { get; }
    delegate void IntervalElapsedCallback();
  
}

internal class PeriodicWorker : IPeriodicWorker
{
    private CancellationTokenSource cancellationToken;
    private Task longRunningTask;
    private IPeriodicWorker.IntervalElapsedCallback job;
    private int interval_ms;
    public bool IsRunning => longRunningTask?.Status == TaskStatus.Running;


    public void Start(IPeriodicWorker.IntervalElapsedCallback callback, int interval)
    {
        interval_ms = interval;
        job += callback;
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
            job?.Invoke();

            Thread.Sleep(interval_ms);
        }
    }

    
}
