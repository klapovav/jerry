using Jerry.Coordinates;
using Jerry.Extensions;
using Slave;
using System;
using System.Collections.Concurrent;
using System.Drawing;

namespace Jerry.Connection.Gatekeeper;

public enum System
{
    Windows = 0,
    Linux = 1,
    MacOS = 2,
    Mock = 3,
}

public class ClientValidInfo
{
    private readonly ErrorLeadingToDataCorrection warning;

    private Guid guid;
    public String Name { get; private set; }
    public Size Resolution { get; private set; }
    public System OS { get; private set; }

    public ErrorLeadingToDataCorrection Warning => warning;

    public Guid Guid
    {
        get { return guid; }
        set
        {
            warning.Add(ErrorLeadingToDataCorrection.GuidAlreadyUsed);
            guid = value;
        }
    }

    public LocalCoordinate Cursor
    {
        get; private set;
    }

    private static Guid GenerateUniqueGuid(ConcurrentDictionary<Guid, Ticket> clients, Guid localID)
    {
        var guid = Guid.NewGuid();
        while (localID == guid || clients.ContainsKey(guid))
        {
            guid = Guid.NewGuid();
        }
        return guid;
    }

    public ClientValidInfo(ClientInfo original, ConcurrentDictionary<Guid, Ticket> clients, Guid localID)
    {
        warning = ErrorLeadingToDataCorrection.None;
        Resolution = new Size(original.Width, original.Height);
        Name = original.Name;

        if (!Guid.TryParse(original.Guid.Value, out Guid used))
        {
            warning = warning.Add(ErrorLeadingToDataCorrection.GuidInvalid);
            used = Guid.NewGuid();
        }
        else if (localID == used || clients.ContainsKey(used))
        {
            warning = warning.Add(ErrorLeadingToDataCorrection.GuidAlreadyUsed);
            used = GenerateUniqueGuid(clients, localID);
        }
        guid = used;

        var x = original?.Cursor?.X ?? original?.Width / 2 ?? 0;
        var y = original?.Cursor?.Y ?? original?.Height / 2 ?? 0;
        y = Math.Min(Math.Max(0, y), original.Height - 1);
        x = Math.Min(Math.Max(0, x), original.Width - 1);

        if (y != original.Cursor?.Y || x != original.Cursor?.X)
        {
            warning = warning.Add(ErrorLeadingToDataCorrection.MousePositionOutOfBounds);
        }

        Cursor = new LocalCoordinate(x, y);
        OS = original.System switch
        {
            Slave.ClientInfo.Types.OS.Mac => System.MacOS,
            Slave.ClientInfo.Types.OS.Windows => System.Windows,
            Slave.ClientInfo.Types.OS.Linux => System.Linux,
            _ => System.Mock,
        };
    }
}