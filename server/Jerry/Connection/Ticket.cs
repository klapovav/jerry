namespace Jerry;

/// <summary>
/// Each instance corresponds to an identifier for a successful handshake.
/// </summary>
public readonly struct Ticket
{
    public int ID { get; }

    public override readonly int GetHashCode() => ID;

    public Ticket(int id)
    {
        ID = id;
    }

    public static bool operator ==(Ticket t1, Ticket t2)
    {
        return t1.Equals(t2);
    }

    public static bool operator !=(Ticket t1, Ticket t2)
    {
        return !t1.Equals(t2);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is Ticket other)
            return ID.Equals(other.ID);
        return false;
    }
}