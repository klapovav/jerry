using Jerry.ConfigurationManager;
using Jerry.Controllable;
using Jerry.Coordinates;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Jerry.LayoutExt.Screen;

internal class LocalLayout : IVirtualDesktopLayout
{
    private readonly List<ScreenSimple> LocalScreens = new();
    private readonly ScreenSimple Primary = null;
    public LayoutCoordinate Origin { get; private set; }
    public Ticket ID { get; }
    public int Right { get; private set; }
    public LayoutCoordinate RightTopCorner { get; private set; }

    public LocalLayout(Ticket id, IList<MonitorInfo> mons)
    {
        ID = id;
        foreach (var mon in mons)
        {
            var bounds = FromMonitorInfo(mon);
            var screen = new ScreenSimple(bounds, mon.Name);
            LocalScreens.Add(screen);
            if (mon.isPrimary)
            {
                Primary = screen;
            }
        }
        if (Primary is null)
            throw new ArgumentNullException();
        Origin = new(0, 0);// Primary.Position.Location
        var rtmon = LocalScreens
            .Aggregate((max, next) => max.Position.Right < next.Position.Right ? next : max);
        Right = rtmon.Position.Right - 1;
        RightTopCorner = new LayoutCoordinate(rtmon.Position.Right - 1, rtmon.Position.Top);
    }

    private static Rectangle FromMonitorInfo(MonitorInfo mon) =>
        new(mon.position.X, mon.position.Y, (int)mon.size.width, (int)mon.size.height);

    public bool Contains(ICoordinate pt) => (LocalScreens.Where(ss => ss.Contains(pt)).Any());

    public LayoutCoordinate GetIntersection(LayoutCoordinate lineSegmentStart, LayoutCoordinate lineSegmentEnd)
    {
        foreach (ScreenSimple s in LocalScreens)
        {
            if (s.TryGetIntersection(lineSegmentStart, lineSegmentEnd, out var inter))
            {
                return inter;
            }
        }
        return default;
    }
}