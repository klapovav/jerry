using Jerry.LayoutExt;
using Jerry.Coordinates;
using System.Drawing;

namespace Jerry.Controllable;

public class ScreenSimple
{
    public Rectangle Position { get; private set; }
    public string Name { get; private set; }

    public ScreenSimple(Rectangle bounds, string name)
    {
        Position = bounds;
        Name = name;
    }

    public ScreenSimple(Size resolution, string name)
    {
        var primaryScreenPosition = new Point(0, 0);
        Position = new Rectangle(primaryScreenPosition, resolution);
        Name = name;
    }

    public bool Contains(ICoordinate coordinate)
    {
        return Position.Contains(coordinate.IntoPoint);
    }

    public bool TryGetIntersection(LayoutCoordinate lineSegmentStart, LayoutCoordinate lineSegmentEnd, out LayoutCoordinate inter)
    {
        //  00   01
        //  10   11
        Vector coor_00 = new(Position.Left, Position.Top);
        Vector coor_01 = new(Position.Right, Position.Top);
        Vector coor_10 = new(Position.Left, Position.Bottom);
        Vector coor_11 = new(Position.Right, Position.Bottom);
        inter = new(0, 0);

        var start = new Vector(lineSegmentStart.IntoPoint);
        var end = new Vector(lineSegmentEnd.IntoPoint);

        LineSegment top = new(coor_00, coor_01);
        LineSegment right = new(coor_01, coor_11);
        LineSegment bottom = new(coor_10, coor_11);
        LineSegment left = new(coor_00, coor_10);

        if (Intersects(left, start, end, out Vector intersection))
        {
            inter = new LayoutCoordinate(Position.Left, intersection.DY);
            return true;
        }
        if (Intersects(right, start, end, out intersection))
        {
            inter = new LayoutCoordinate(Position.Right, intersection.DY);
            return true;
        }

        if (Intersects(top, start, end, out intersection))
        {
            inter = new LayoutCoordinate(intersection.DX, Position.Top);
            return true;
        }
        if (Intersects(bottom, start, end, out intersection))
        {
            inter = new LayoutCoordinate(intersection.DX, Position.Bottom);
            return true;
        }
        return false;
    }


    private static bool Intersects(LineSegment a, Vector b1, Vector b2, out Vector intersection)
    {
        Vector a1 = a.Start;
        Vector a2 = a.End;
        intersection = new Vector(0, 0);

        Vector b = a2 - a1;
        Vector d = b2 - b1;
        float bDotDPerp = b.DX * d.DY - b.DY * d.DX;

        if (bDotDPerp == 0)
            return false;

        Vector c = b1 - a1;
        float t = (c.DX * d.DY - c.DY * d.DX) / bDotDPerp;
        if (t < 0 || t > 1)
            return false;

        float u = (c.DX * b.DY - c.DY * b.DX) / bDotDPerp;
        if (u < 0 || u > 1)
            return false;

        intersection = a1 + b * t;

        return true;
    }
}

public struct LineSegment
{
    public LineSegment(Vector start, Vector end)
    {
        Start = start;
        End = end;
    }

    public Vector Start;
    public Vector End;
}