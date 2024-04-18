using Jerry.Coordinates;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace Jerry.Controllable;

/// <summary>
/// Interface for computers capable of receiving user input.
/// </summary>
public interface IControllableComputer : IEquatable<IControllableComputer>
{
    string Name { get; }
    string OS { get; }

    public Guid ID { get; }

    /// <summary>
    /// Identifies a successful connection of a controllable computer to the system.
    /// </summary>
    public Ticket Ticket { get; }

    LocalCoordinate CursorPosition { get; }

    /// <summary>
    /// Method for changing the mouse movement mode between absolute and relative.
    /// </summary>
    void ToogleMouseMode();

    void OnMouseMove(int dx, int dy);

    void OnMouseClick(Events.MouseButton ev);

    void OnMouseWheel(Events.MouseWheel ev);

    void OnKeyEvent(Events.KeyboardHookEvent keyEvent);

    bool OnDeactivate([MaybeNullWhen(false)] out Common.Clipboard clipboard);

    void OnActivate(Common.Clipboard? clipboard);

    void ReleaseModifiers(ModifierKeys modifiers);

    /// <summary>
    /// Send a heartbeat message to check the connection status.
    /// </summary>
    /// <returns>
    /// True if the computer is able to receive messages; otherwise, false.
    /// </returns>
    bool TrySendHeartbeat();

    bool IEquatable<IControllableComputer>.Equals(IControllableComputer? other) => Ticket.Equals(other?.Ticket);

    bool EqualsTicket(Ticket other) => Ticket.Equals(other);
}