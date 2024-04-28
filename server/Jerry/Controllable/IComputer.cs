using Jerry.Coordinates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jerry.Controllable;
public interface IComputer : IEquatable<IComputer>
{
    string Name { get; }
    string OS { get; }

    public Guid ID { get; }

    /// <summary>
    /// Identifies a successful connection of a controllable computer to the system.
    /// </summary>
    public Ticket Ticket { get; }

    LocalCoordinate CursorPosition { get; }

    bool IEquatable<IComputer>.Equals(IComputer? other) => Ticket.Equals(other?.Ticket);

    bool HasTicket(Ticket other) => Ticket.Equals(other);
}
