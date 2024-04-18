namespace Jerry.Coordinates;

public interface ICoordinate
{
    int X { get; }
    int Y { get; }

    System.Drawing.Point IntoPoint { get; }
}