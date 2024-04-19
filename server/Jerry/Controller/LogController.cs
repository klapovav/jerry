using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jerry.Controller;

public class LogController
{
    private static readonly Lazy<LogController> lazyInstance =
        new Lazy<LogController>(() => new LogController());

    private LogController()
    {
        //It's essential to allocate the console window before
        //initializing the logger to ensure proper functionality.
        LoggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        ConsoleWindow = new ConsoleWindow(true);

        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LoggingLevelSwitch)
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}")
            .WriteTo.File("log/", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}")
            .CreateLogger();
    }

    public static LogController Instance
    {
        get
        {
            return lazyInstance.Value;
        }
    }
    public LoggingLevelSwitch LoggingLevelSwitch { get; }
    public ConsoleWindow ConsoleWindow { get; }
}

