using System.Drawing;

namespace Jerry.Coordinates;

public struct LocalCoordinate : ICoordinate
{
    public LocalCoordinate(int x, int y)
    {
        pt = new Point(x, y);
    }

    private Point pt;

    public readonly int X => pt.X;
    public readonly int Y => pt.Y;

    public readonly Point IntoPoint => pt;
}