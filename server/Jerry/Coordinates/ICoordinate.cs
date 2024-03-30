namespace Jerry.Coordinates;

public interface ICoordinate
{
    int X { get; }
    int Y { get; }

    System.Drawing.Point IntoPoint { get; }
    //public static ICoordinate operator+(ICoordinate coordinate, IVector move)
    //{
    //    coordinate.X += move.DX;
    //    coordinate.Y += move.DY;
    //    return new Coordinate { coordinate};
    //}
}