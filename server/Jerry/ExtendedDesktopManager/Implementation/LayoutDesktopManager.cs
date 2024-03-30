using Jerry.Hook;
using Jerry.LayoutExt;
using Jerry.Coordinates;
using System;
using Jerry.Connection;

namespace Jerry.ExtendedDesktopManager;

internal class Transition
{
    public TransitionType moveType;
    public LayoutCoordinate leavingPosition;
    public LayoutCoordinate initialPosition;
    public Ticket hoveredScreen;
}

public enum TransitionType
{
    InsideActiveScreenArea = 1,
    UnallocatedArea = 2,
    RemoteToLocal = 4,
    LocalToRemote = 5,
    RemoteToRemote = 6,
}

internal class LayoutDesktopManager : BaseDesktopManager
{
    private readonly Layout ExtDesktopLayout;
    public override Mode Mode => Mode.Layout;

    public LayoutDesktopManager(Action<Strategy> onActiveChanged) : base(onActiveChanged)
    {
        ExtDesktopLayout = new(base.LocalComputer.Ticket);
    }

    public override void RegisterClient(ConnectedClient proxy)
    {
        ExtDesktopLayout.AddRemote(proxy);
        base.RegisterClient(proxy);
    }

    public override void DisconnectClient(Ticket disc_id)
    {
        ExtDesktopLayout.Remove(disc_id);
        base.DisconnectClient(disc_id);
    }

    public override void OnMouseEvent(Events.MouseDeltaMove mouseMove)
    { 
        var previousCoordinate = ExtDesktopLayout.GetCursorPositionInLayout(Active);
        var newCoordinateCandidate = previousCoordinate + mouseMove;
        var transition = PeekNewPosition(previousCoordinate, newCoordinateCandidate);

        switch (transition.moveType)
        {
            case TransitionType.InsideActiveScreenArea: 
            case TransitionType.UnallocatedArea:
                //V1
                base.OnMouseEvent(mouseMove);
                break;
            case TransitionType.RemoteToRemote:
                var remote = ExtDesktopLayout.LayoutCoordinateToLocal(transition.leavingPosition);
                Active.OnMouseMove(remote.X, remote.Y);
                base.SwitchTo(transition.hoveredScreen);
                var init = ExtDesktopLayout.LayoutCoordinateToLocal(transition.initialPosition);
                Active.OnMouseMove(init.X, init.Y);
                break;
            case TransitionType.RemoteToLocal:
                var remote2 = ExtDesktopLayout.LayoutCoordinateToLocal(transition.leavingPosition);
                Active.OnMouseMove(remote2.X, remote2.Y);
                base.SwitchTo(transition.hoveredScreen);
                var init2 = ExtDesktopLayout.LayoutCoordinateToLocal(transition.initialPosition);
                Active.OnMouseMove(init2.X, init2.Y);
                break;
            case TransitionType.LocalToRemote:
                break;
            default:
                break;
        }
    }

    private Transition PeekNewPosition(LayoutCoordinate previous, LayoutCoordinate candidate) 
    {
        var transition = new Transition
        {
            leavingPosition = candidate
        };
        // STANDARD InsideActiveScreen
        if (ExtDesktopLayout.AreAssociated(Active.Ticket, candidate))
        {
            transition.moveType = TransitionType.InsideActiveScreenArea;
            return transition;
        }

        // switch
        if (ExtDesktopLayout.TryGetMonitorAssociatedWith(candidate, out transition.hoveredScreen))
        {
            transition.moveType = GetScreenChangeType(Active.Ticket, transition.hoveredScreen);
            transition.leavingPosition = ExtDesktopLayout.GetIntersection(Active.Ticket, previous, candidate);
            transition.initialPosition = ExtDesktopLayout.GetIntersection(transition.hoveredScreen, previous, candidate);
            return transition;
        }

        // trim
        transition.moveType = TransitionType.UnallocatedArea;
        //transition.leavingPosition = id.trim(candidate);
        return transition;
    }


    private TransitionType GetScreenChangeType(Ticket from, Ticket to)
    {
        return (ExtDesktopLayout.IsLocal(from), ExtDesktopLayout.IsLocal(to)) switch
        {
            (true, false) => TransitionType.LocalToRemote,
            (false, true) => TransitionType.RemoteToLocal,
            (false, false) => TransitionType.RemoteToRemote,
            _ => throw new NotImplementedException(),
        };
    }
}