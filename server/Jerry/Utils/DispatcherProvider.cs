using Serilog.Core;
using System;
using System.Windows.Threading;

namespace Jerry;

public static class Constants
{
    public const UInt16 JerryServerID = 23888;
    public const UInt16 JerryClientID = 23889;
}

public sealed class DispatcherProvider
{
    private DispatcherProvider(Dispatcher UIDispatcher, LoggingLevelSwitch s)
    {
        uiDispatcher = UIDispatcher;
        lls = s;
    }
    private readonly LoggingLevelSwitch? lls = null;

    private static DispatcherProvider? instance = null;
    private readonly Dispatcher uiDispatcher;

    public static void Init(Dispatcher hookCallbackDispatcher, LoggingLevelSwitch s)
    {
        if (hookCallbackDispatcher is null) { throw new ArgumentNullException(nameof(hookCallbackDispatcher)); }

        if (instance is not null)
            throw new InvalidOperationException();
        instance = new DispatcherProvider(hookCallbackDispatcher, s);
    }

    public static Dispatcher HookCallbackDispatcher => instance?.uiDispatcher ?? throw new InvalidOperationException();
    public static LoggingLevelSwitch LoggingLevelSwitch => instance?.lls ?? throw new InvalidOperationException();
}