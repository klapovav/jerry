using System;
using System.Threading;
using System.Windows.Threading;

namespace Jerry;

public static class Constants
{
    public  const UInt16 JerryServerID = 23888;
    public  const UInt16 JerryClientID = 23889;
}
public sealed class DispatcherProvider
{
    private DispatcherProvider(Dispatcher UIDispatcher)
    {
        uiDispatcher = UIDispatcher;
    }

    private static DispatcherProvider instance = null;
    private readonly Dispatcher uiDispatcher;

    public static void Init(Dispatcher hookCallbackDispatcher)
    {
        if (hookCallbackDispatcher is null) { throw new ArgumentNullException(nameof(hookCallbackDispatcher)); }

        if (instance is not null)
            throw new InvalidOperationException();
        instance = new DispatcherProvider(hookCallbackDispatcher);
    }
 

    public static Dispatcher HookCallbackDispatcher => instance?.uiDispatcher ?? throw new InvalidOperationException();
}