using System;
using System.Collections.Generic;
using Sol.Helpers;

namespace Sol.Models;

public enum ComputerSessionType
{
    Console,
    RemoteDesktop,
    Disconnected,
    Other
}

/// <summary>
/// Represents an active or disconnected logon session on a computer endpoint.
/// </summary>
public record ComputerSessionInfo
{
    public uint? SessionId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public string SamAccountName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public ComputerSessionType SessionType { get; init; } = ComputerSessionType.Console;
    public DateTime? LogonTime { get; init; }
    public bool IsActive { get; init; } = true;

    public string FullUsername => !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(SamAccountName)
        ? $"{Domain}\\{SamAccountName}"
        : (!string.IsNullOrWhiteSpace(SamAccountName) ? SamAccountName : Username);

    public string EffectiveDisplayName => !string.IsNullOrWhiteSpace(DisplayName)
        ? DisplayName
        : (!string.IsNullOrWhiteSpace(SamAccountName) ? SamAccountName : Username);

    public string SessionTypeText => SessionType switch
    {
        ComputerSessionType.Console => Strings.S.SessionTypeConsole,
        ComputerSessionType.RemoteDesktop => Strings.S.SessionTypeRdp,
        ComputerSessionType.Disconnected => Strings.S.SessionTypeDisconnected,
        _ => Strings.S.SessionTypeConsole
    };

    public bool IsConsole => SessionType == ComputerSessionType.Console;
    public bool IsRdp => SessionType == ComputerSessionType.RemoteDesktop;
    public bool IsDisconnected => SessionType == ComputerSessionType.Disconnected;

    public string FormattedLogonTime => LogonTime?.ToString("g") ?? "—";

    public string FormattedDuration
    {
        get
        {
            if (LogonTime == null) return "—";
            var duration = DateTime.Now - LogonTime.Value;
            if (duration.TotalMinutes < 1) return Strings.S.SessionDurationJustNow;
            return Strings.FormatDuration(duration);
        }
    }

    public string FormattedSessionSummary => LogonTime.HasValue
        ? Strings.FormatSessionSince(FormattedLogonTime)
        : SessionTypeText;

    public string CopyDetailsText => $"{EffectiveDisplayName} ({FullUsername}) - {SessionTypeText}, {FormattedSessionSummary}";
}

/// <summary>
/// Diagnostic snapshot of all active and disconnected logon sessions on a target computer.
/// </summary>
public record ComputerSessionSnapshot
{
    public string Hostname { get; init; } = string.Empty;
    public List<ComputerSessionInfo> Sessions { get; init; } = [];
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime QueriedAt { get; init; } = DateTime.Now;

    public bool HasActiveSessions => Sessions.Count > 0;
    public string ActiveSessionsCountBadge => Sessions.Count.ToString();
}
