using System.Drawing;

namespace Jerry.Coordinates;

public readonly struct MonitorResolution
{
    public MonitorResolution(int width, int height)
    {
        Size = new(width, height);
    }

    public MonitorResolution(Size size)
    {
        Size = size;
    }

    public Size Size { get; }
    public int X => Size.Width;
    public int Y => Size.Height;
}