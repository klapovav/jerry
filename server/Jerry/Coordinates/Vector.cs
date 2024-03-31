using System;

namespace Jerry.Coordinates;

public class Vector : IVector
{
    //short?
    public int DX { get; private set; }

    public int DY { get; private set; }

    public int Lenght => Math.Max(Math.Abs(DX), Math.Abs(DY));

    public Vector(int x, int y)
    {
        DX = x;
        DY = y;
    }

    public Vector(System.Drawing.Point pt)
    {
        DX = pt.X;
        DY = pt.Y;
    }

    public Vector(ICoordinate from, ICoordinate to)
    {
        _ = from ?? throw new ArgumentNullException(nameof(from));
        _ = to ?? throw new ArgumentNullException(nameof(to));

        DX = to.X - from.X;
        DY = to.Y - from.Y;
    }

    public static Vector operator +(Vector a, Vector b)
    {
        _ = a ?? throw new ArgumentNullException(nameof(a));
        _ = b ?? throw new ArgumentNullException(nameof(b));

        return new Vector(a.DX + b.DX, a.DY + b.DY);
    }

    public static Vector operator -(Vector a, Vector b)
    {
        _ = a ?? throw new ArgumentNullException(nameof(a));
        _ = b ?? throw new ArgumentNullException(nameof(b));

        return new Vector(a.DX - b.DX, a.DY - b.DY);
    }

    public static Vector operator /(Vector a, int b)
    {
        _ = a ?? throw new ArgumentNullException(nameof(a));

        return new Vector(a.DX / b, a.DY / b);
    }

    public static Vector operator *(Vector a, float b)
    {
        _ = a ?? throw new ArgumentNullException(nameof(a));

        return new Vector((int)(a.DX * b), (int)(a.DY * b));
    }
}