using System;

namespace Jerry.Connection.Gatekeeper;

public struct HandshakeResult
{
    public bool Succeeded;
    public Rejection RejectionType;
    public ErrorLeadingToDataCorrection Warnings;
    public ClientValidInfo RepairedInfo;

    public HandshakeResult(Rejection rejectionType)
    {
        Succeeded = false;
        RejectionType = rejectionType;
        Warnings = ErrorLeadingToDataCorrection.None;
        RepairedInfo = null;
    }

    public HandshakeResult(ClientValidInfo info, ErrorLeadingToDataCorrection warning)
    {
        Succeeded = true;
        RejectionType = Rejection.None;
        Warnings = warning;
        RepairedInfo = info;
    }
}

public enum Rejection
{
    None,
    Unknown,
    KeyExchangeFailed,
    InitialInfoMissing,
    UnexpectedResolution,
    WrongPassword,
}

[Flags]
public enum ErrorLeadingToDataCorrection
{
    None = 0,
    MousePositionOutOfBounds = 1,
    GuidInvalid = 2,
    GuidAlreadyUsed = 4,
}