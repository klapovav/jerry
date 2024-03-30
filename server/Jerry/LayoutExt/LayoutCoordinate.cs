using Jerry.Coordinates;
using System;
using System.Drawing;

namespace Jerry.LayoutExt
{
    public struct LayoutCoordinate : ICoordinate
    {
        public LayoutCoordinate(int x, int y)
        {
            pt = new Point(x, y);
        }

        public readonly int X => pt.X;
        public readonly int Y => pt.Y;
        private Point pt;

        public readonly Point IntoPoint => pt;

        public static LayoutCoordinate operator +(LayoutCoordinate a, IVector b)
        {
            _ = b ?? throw new ArgumentNullException(nameof(b));
            return new LayoutCoordinate(a.X + b.DX, a.Y + b.DY);
        }
    }
}