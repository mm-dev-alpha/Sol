using System;
using System.Collections.Generic;
using Sol.Helpers;

namespace Sol.Models;

/// <summary>
/// Represents the uptime, last boot time, and pending reboot diagnostic status of a computer.
/// </summary>
public record ComputerUptimeSnapshot
{
    public string Hostname { get; init; } = string.Empty;
    public DateTime? LastBootUpTime { get; init; }
    public TimeSpan? Uptime { get; init; }
    public bool IsRebootPending { get; init; }
    public bool IsRebootStatusKnown { get; init; } = true;
    public List<string> PendingRebootReasons { get; init; } = [];
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime QueriedAt { get; init; } = DateTime.Now;

    public string FormattedLastBoot => LastBootUpTime?.ToString("g") ?? "—";

    public string FormattedUptime
    {
        get
        {
            if (Uptime == null) return "—";
            var ts = Uptime.Value;
            if (ts.TotalDays >= 1)
            {
                return Strings.FormatUptimeDays((int)ts.TotalDays, ts.Hours);
            }
            if (ts.TotalHours >= 1)
            {
                return Strings.FormatUptimeHours(ts.Hours, ts.Minutes);
            }
            return Strings.FormatUptimeMinutes(Math.Max(1, ts.Minutes));
        }
    }

    public string RebootStatusText => !IsRebootStatusKnown
        ? Strings.S.RebootStatusUnknown
        : (IsRebootPending ? Strings.S.RebootRequired : Strings.S.NoRebootRequired);
    public string FormattedRebootReasons => PendingRebootReasons.Count > 0 ? string.Join(", ", PendingRebootReasons) : string.Empty;
}
