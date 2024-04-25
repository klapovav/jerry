using Jerry.Controllable;
using Jerry.Coordinates;
using Jerry.Extensions;
using Microsoft.Windows.Themes;
using Slave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Jerry.Connection.Gatekeeper;

public enum System
{
    Windows = 0,
    Linux = 1,
    MacOS = 2,
    Mock = 3,
}


public class ClientInfo 
{
    public ClientInfo(Slave.ClientInfo original, Guid guid, LocalCoordinate cursor)
    {
        Name = original.Name;
        Guid = guid;
        Resolution = new Size(original.Width, original.Height);
        OS = original.System switch
        {
            Slave.ClientInfo.Types.OS.Mac => System.MacOS,
            Slave.ClientInfo.Types.OS.Windows => System.Windows,
            Slave.ClientInfo.Types.OS.Linux => System.Linux,
            _ => System.Mock,
        };
        Cursor = cursor;
    }
    public String Name { get; init; }
    public Size Resolution { get; init; }
    public System OS { get; init; }
    public Guid Guid { get; init; }
    public LocalCoordinate Cursor { get; init; }

}


