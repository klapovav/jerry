using Jerry.Controllable;
using Jerry.Coordinates;
using System.Drawing;

namespace Jerry.LayoutExt.Screen;

internal class RemoteLayout : IVirtualDesktopLayout
{
    private readonly ScreenSimple Primary = null;

    public RemoteLayout(Size resolution, LayoutCoordinate position, string name, Ticket id)
    {
        var rect = new Rectangle(position.IntoPoint, resolution);
        Primary = new ScreenSimple(rect, name);
        ID = id;
        Origin = position;
    }

    public Ticket ID { get; }

    public LayoutCoordinate RightTopCorner => new LayoutCoordinate(Primary.Position.Right - 1, Primary.Position.Top);

    public int Right => Primary.Position.Right - 1;

    public LayoutCoordinate Origin { get; private set; }

    public bool Contains(ICoordinate pt) => Primary.Contains(pt);

    public LayoutCoordinate GetIntersection(LayoutCoordinate lineSegmentStart, LayoutCoordinate lineSegmentEnd)
    {
        if (Primary.TryGetIntersection(lineSegmentStart, lineSegmentEnd, out LayoutCoordinate result))
        {
            return result;
        }
        return default;
    }
}