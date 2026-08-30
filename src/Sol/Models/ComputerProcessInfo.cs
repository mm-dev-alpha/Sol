using System;
using System.Collections.Generic;
using System.Linq;

namespace Sol.Models;

/// <summary>
/// Represents a running process on a target computer endpoint.
/// </summary>
public record ComputerProcessInfo
{
    public uint ProcessId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public ulong WorkingSetBytes { get; init; }
    public double CpuUsagePercent { get; init; }
    public double NetworkMbps { get; init; }
    public string Owner { get; init; } = string.Empty;
    public DateTime? CreationDate { get; init; }

    /// <summary>
    /// Critical Windows OS core processes that must not be killed remotely.
    /// </summary>
    public static readonly HashSet<string> CriticalProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System",
        "Registry",
        "smss.exe",
        "csrss.exe",
        "wininit.exe",
        "services.exe",
        "lsass.exe",
        "winlogon.exe",
        "dwm.exe",
        "fontdrvhost.exe"
    };

    public static bool IsCriticalProcess(uint processId, string? name, string? owner = null)
    {
        if (processId <= 4) return true;
        if (string.IsNullOrWhiteSpace(name)) return false;

        string exeName = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe";
        if (CriticalProcessNames.Contains(name) || CriticalProcessNames.Contains(exeName)) return true;

        if (string.Equals(exeName, "svchost.exe", StringComparison.OrdinalIgnoreCase) &&
            (owner == null || string.Equals(owner, "NT AUTHORITY\\SYSTEM", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    public bool IsCriticalSystemProcess => IsCriticalProcess(ProcessId, Name, Owner);

    public bool CanTerminate => !IsCriticalSystemProcess;

    public string FormattedMemory
    {
        get
        {
            if (WorkingSetBytes == 0) return "0 MB";
            double mb = WorkingSetBytes / (1024.0 * 1024.0);
            if (mb >= 1024.0)
            {
                return $"{mb / 1024.0:F2} GB";
            }
            return $"{mb:F1} MB";
        }
    }

    public double MemoryMb => WorkingSetBytes / (1024.0 * 1024.0);

    public string FormattedCpu => CpuUsagePercent > 0.05 ? $"{CpuUsagePercent:F1} %" : "0.0 %";

    public string FormattedNetwork => NetworkMbps > 0.05 ? $"{NetworkMbps:F1} Mbps" : "0.0 Mbps";

    public string FormattedCreationDate => CreationDate?.ToString("g") ?? "—";

    public string DisplayOwner
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Owner)) return Owner;
            if (ProcessId <= 4 || IsCriticalSystemProcess) return "NT AUTHORITY\\SYSTEM";
            return "—";
        }
    }
}

/// <summary>
/// Diagnostic snapshot containing all running processes on a target workstation.
/// </summary>
public record ComputerProcessSnapshot
{
    public string Hostname { get; init; } = string.Empty;
    public List<ComputerProcessInfo> Processes { get; init; } = [];
    public bool IsSuccess { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;

    public int TotalProcessCount => Processes.Count;

    public ulong TotalMemoryUsageBytes => Processes.Aggregate(0UL, (sum, p) => sum + p.WorkingSetBytes);

    public string FormattedTotalMemory
    {
        get
        {
            double totalMb = TotalMemoryUsageBytes / (1024.0 * 1024.0);
            if (totalMb >= 1024.0)
            {
                return $"{totalMb / 1024.0:F2} GB";
            }
            return $"{totalMb:F1} MB";
        }
    }
}
