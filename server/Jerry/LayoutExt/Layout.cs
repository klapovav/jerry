using Jerry.ConfigurationManager;
using Jerry.Controllable;
using Jerry.Coordinates;
using Jerry.LayoutExt.Screen;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jerry.LayoutExt
{
    internal class Layout
    {
        private readonly LocalLayout LocalComputer;
        private readonly List<IVirtualDesktopLayout> Disconnected;
        private readonly Dictionary<Ticket, IVirtualDesktopLayout> Screens;

        public Layout(Ticket localID)
        {
            Screens = new Dictionary<Ticket, IVirtualDesktopLayout>();
            var mons = MonitorInfoProvider.GetScreensWin();
            LocalComputer = new LocalLayout(localID, mons);
            Screens.Add(localID, LocalComputer);
            Disconnected = new List<IVirtualDesktopLayout>();
        }

        public bool IsLocal(Ticket id)
        {
            return id == LocalComputer.ID;
        }

        public void AddRemote(ConnectedClient init)
        {
            var pos = GetInitPosition();
            Log.Debug("Clients[{@Ticket}] inital pos:  {0}x{1}.", init.ID, pos.X, pos.Y);
            var remote = new RemoteLayout(init.Info.Resolution, pos, init.Info.Name, init.ID);
            if (!Screens.TryAdd(remote.ID, remote))
            { throw new Exception("Layout.Screens.TryAdd failed"); }
        }

        public void Remove(Ticket id)
        {
            if (id == LocalComputer.ID)
            { return; }

            if (Screens.Remove(id, out IVirtualDesktopLayout disconnected))
            {
                Disconnected.Add(disconnected);
                return;
            }
            throw new Exception("Layout.Remove failed");
        }

        public LayoutCoordinate GetCursorPositionInLayout(IControllableComputer client)
        {
            var monitorCoordinate = client.CursorPosition;
            var monitorPosition = Screens[client.Ticket].Origin;
            return Add(monitorPosition, monitorCoordinate);
        }

        public LocalCoordinate LayoutCoordinateToLocal(LayoutCoordinate coord)
        {
            var mon = Screens
                .Where(pair => pair.Value.Contains(coord))
                .Select(pair => pair.Value)
                .FirstOrDefault() ?? throw new Exception("");
            var vec = new Vector(mon.Origin, coord);
            return new LocalCoordinate(vec.DX, vec.DY);
        }

        public LayoutCoordinate GetIntersection(Ticket id, LayoutCoordinate from, LayoutCoordinate to)
        {
            return Screens[id].GetIntersection(from, to);
        }

        public bool AreAssociated(Ticket monitorID, LayoutCoordinate point)
        {
            return Screens[monitorID].Contains(point);
        }

        public bool TryGetMonitorAssociatedWith(LayoutCoordinate point, out Ticket monitorID)
        {
            var mon = Screens
                .Where(pair => pair.Value.Contains(point))
                .Select(pair => pair.Value)
                .FirstOrDefault();
            monitorID = mon?.ID ?? new Ticket(-1);
            return mon is not null;
        }

        private static LayoutCoordinate Add(LayoutCoordinate origin, LocalCoordinate localCoordinate)
        {
            var x = origin.X + localCoordinate.X;
            var y = origin.Y + localCoordinate.Y;
            return new(x, y);
        }

        private LayoutCoordinate GetInitPosition()
        {
            var rt = Screens
                .Select(pair => pair.Value)
                .Aggregate((max, next) => max.Right < next.Right ? next : max)
                .RightTopCorner;
            return new LayoutCoordinate(rt.X + 1, rt.Y);
        }
    }
}