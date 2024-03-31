using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Jerry.ConfigurationManager;

[StructLayout(LayoutKind.Sequential)]
public record PhysicalPosition
{
    public Int32 X;
    public Int32 Y;
}
[StructLayout(LayoutKind.Sequential)]
public record PhysicalSize
{
    public UInt32 width;
    public UInt32 height;
}
[StructLayout(LayoutKind.Sequential)]
public record Monitor
{
    public bool some;
    public PhysicalSize size;
    public PhysicalPosition position;
    public double scale_factor;
}
public record MonitorInfo : Monitor
{
    public string Name;
    public bool isPrimary;
    public MonitorInfo(Monitor monitor, Screen screen)
    {
        Name = screen.DeviceName;
        isPrimary = screen.Primary;
        position = monitor.position;
        size = monitor.size;
        some = monitor.some;
        scale_factor = monitor.scale_factor;
    }

    public MonitorInfo(Screen screen)
    {
        Name = screen.DeviceName;
        isPrimary = screen.Primary;
        position = new PhysicalPosition() { X = screen.Bounds.X, Y = screen.Bounds.Y };
        size = new PhysicalSize()
        {
            height = (UInt32)screen.Bounds.Height,
            width = (UInt32)screen.Bounds.Width
        };
        some = true;
        scale_factor = 666;
    }
}