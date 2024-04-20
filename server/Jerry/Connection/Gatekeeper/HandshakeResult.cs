using System;
using System.ComponentModel.DataAnnotations;

namespace Jerry.Connection.Gatekeeper;

/// <summary>
/// Result of the handshake process between a client and a server.
/// </summary>
public struct HandshakeResult
{
    /// <summary>
    /// Indicates whether the handshake process succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the reason why the handshake process failed; returns <see cref="Rejection.None"/> if the handshake succeeded.
    /// </summary>
    public Rejection RejectionType { get; }
    ///<summary>
    /// Gets the less severe issues encountered during a successful handshake process, leading to data correction.
    /// </summary>
    public FixableIssue Warnings { get; }
    /// <summary>
    /// Contains parameters of the client that has been successfully accepted by the server.
    /// If <see cref="Succeeded"/> is false, this field is null.
    /// </summary>
    public ClientValidInfo? RepairedInfo;

    public HandshakeResult(Rejection rejectionType)
    {
        Succeeded = false;
        RejectionType = rejectionType;
        Warnings = FixableIssue.None;
        RepairedInfo = null;
    }

    public HandshakeResult(ClientValidInfo info, FixableIssue warning)
    {
        Succeeded = true;
        RejectionType = Rejection.None;
        Warnings = warning;
        RepairedInfo = info;
    }
}
/// <summary>
/// Specifies the irrecoverable errors that may occur during a handshake.
/// </summary>
public enum Rejection
{
    None,
    [Display(Name = "Unknown")]
    Unknown,
    [Display(Name = "Key exchange failed")]
    KeyExchangeFailed,
    [Display(Name = "Client error: Initial message missing")]
    InitialInfoMissing,
    [Display(Name = "Client error: Unexpected resolution")]
    UnexpectedResolution,
    [Display(Name = "Wrong password")]
    WrongPassword,
}

[Flags]
public enum FixableIssue
{
    None = 0,
    MousePositionOutOfBounds = 1,
    GuidInvalid = 2,
    GuidAlreadyUsed = 4,
}