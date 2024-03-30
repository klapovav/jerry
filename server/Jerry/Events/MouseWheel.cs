namespace Jerry.Events;

public struct MouseWheel
{
    public Direction ScrollDirection;

    //30 ~ 1 line or character
    //120 ~ 1 step
    public short Amount = 0;

    public MouseWheel(Direction scrollDirection, short amount)
    {
        ScrollDirection = scrollDirection;
        Amount = amount;
    }
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}