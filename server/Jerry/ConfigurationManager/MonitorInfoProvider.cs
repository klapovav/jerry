using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Jerry.ConfigurationManager;

public static class MonitorInfoProvider
{
    [DllImport("glutin_wrapper.dll")]
    private static extern Monitor get_monitor(string name);

    // public static IList<Tuple<Rectangle,String>> GetScreensRectangle()
    // {
    //     return GetScreensWin()
    //         .Select(mon => Tuple.Create(MonToRectangle(mon), mon.Name))
    //         .ToList();
    // }
    //
    // public static Rectangle MonToRectangle(MonitorInfo mon)
    // {
    //     return new Rectangle(mon.position.X, mon.position.Y, (int)mon.size.width, (int)mon.size.height);
    //
    // }
    public static IList<MonitorInfo> GetScreensGlutin()
    {
        var screens = new List<MonitorInfo>();
        foreach (var s in Screen.AllScreens)
        {
            var mon = get_monitor(s.DeviceName);
            if (!mon.some)
                throw new NullReferenceException();
            screens.Add(new MonitorInfo(mon, s));
        }
        return screens;
    }

    public static IList<MonitorInfo> GetScreensWin()
    {
        var screens = new List<MonitorInfo>();
        foreach (var s in Screen.AllScreens)
        {
            screens.Add(new MonitorInfo(s));
        }
        return screens;
    }
}