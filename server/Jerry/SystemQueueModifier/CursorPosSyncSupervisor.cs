using Jerry.Hook.SysGlobalState;
using Jerry.Hook.WinApi;
using System;

namespace Jerry.SystemQueueModifier;

public enum Variability
{
    Static = 0,
    AboutToChange = 1,
}

internal class CursorPosSyncSupervisor
{
    private DateTime creationTime;
    private readonly TimeSpan delay = TimeSpan.FromMilliseconds(50);
    private NativePoint lastCursorPosition;

    public NativePoint? FixedCursorPosition => (CursorPositionVariability == Variability.AboutToChange) ? null : lastCursorPosition;

    public Variability CursorPositionVariability { get; set; }

    public CursorPosSyncSupervisor()
    {
        ExpectMsgInSystemQueue();
    }

    public void ExpectMsgInSystemQueue()
    {
        creationTime = DateTime.Now;
        CursorPositionVariability = Variability.AboutToChange;
    }

    public void Update()
    {
        if (CursorPositionVariability == Variability.Static)
            return;

        if ((DateTime.Now - creationTime) < delay)
            return;
        CursorPositionVariability = Variability.Static;
        creationTime = DateTime.MinValue;
        lastCursorPosition = Cursor.GetCursorPosition();
    }

    public bool TryGetCursorPosition(out NativePoint cursorFixedPosition)
    {
        Update();
        cursorFixedPosition = lastCursorPosition;
        return CursorPositionVariability == Variability.Static;
    }

    public bool InSync => CursorPositionVariability == Variability.Static;
}