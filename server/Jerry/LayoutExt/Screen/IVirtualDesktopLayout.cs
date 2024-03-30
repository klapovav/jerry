using Jerry.Coordinates;

namespace Jerry.LayoutExt.Screen;

/// <summary>
/// "It represents a single computer placed within the layout, which can consist of either one monitor or multiple monitors."
/// </summary>
internal interface IVirtualDesktopLayout
{
    public int Right { get; }
    public Ticket ID { get; }

    public bool Contains(ICoordinate pt);

    public LayoutCoordinate RightTopCorner { get; }
    public LayoutCoordinate Origin { get; }

    public LayoutCoordinate GetIntersection(LayoutCoordinate lineSegmentStart, LayoutCoordinate lineSegmentEnd);
}