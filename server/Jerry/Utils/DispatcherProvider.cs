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
    private DispatcherProvider(Dispatcher UIDispatcher)
    {
        uiDispatcher = UIDispatcher;
    }
    //private readonly LoggingLevelSwitch? lls = null;
    //private readonly ConsoleWindow? consoleWindow = null;

    private static DispatcherProvider? instance = null;
    private readonly Dispatcher uiDispatcher;

    public static void Init(Dispatcher hookCallbackDispatcher)
    {
        if (hookCallbackDispatcher is null) { throw new ArgumentNullException(nameof(hookCallbackDispatcher)); }

        if (instance is not null)
            throw new InvalidOperationException();
        instance = new DispatcherProvider(hookCallbackDispatcher);
    }

    public static Dispatcher HookCallbackDispatcher => instance?.uiDispatcher ?? throw new InvalidOperationException();
    //public static LoggingLevelSwitch LoggingLevelSwitch => instance?.lls ?? throw new InvalidOperationException();
    //public static ConsoleWindow ConsoleWindow => instance?.consoleWindow ?? throw new InvalidOperationException();
}