using System;
using System.Collections.Generic;
using Sol.Helpers;

namespace Sol.Models;

/// <summary>
/// Represents diagnostic and capacity information for a single local logical disk partition.
/// </summary>
public record ComputerDiskDriveInfo
{
    public string DeviceId { get; init; } = string.Empty;       // C:
    public string VolumeName { get; init; } = string.Empty;     // Windows
    public string FileSystem { get; init; } = string.Empty;     // NTFS
    public ulong TotalBytes { get; init; }
    public ulong FreeBytes { get; init; }
    public ulong UsedBytes => TotalBytes >= FreeBytes ? TotalBytes - FreeBytes : 0;

    public double UsedPercentage => TotalBytes > 0 ? ((double)UsedBytes / TotalBytes) * 100.0 : 0;
    public double FreePercentage => TotalBytes > 0 ? ((double)FreeBytes / TotalBytes) * 100.0 : 0;

    public string FormattedTotalSize => FormatBytes(TotalBytes);
    public string FormattedFreeSpace => FormatBytes(FreeBytes);
    public string FormattedUsedSpace => FormatBytes(UsedBytes);

    public string MediaType { get; init; } = "SSD";             // "SSD", "NVMe", "HDD"
    public string HealthStatus { get; init; } = "OK";           // "OK", "Warning", "Pred Fail"

    public bool IsHealthOk => HealthStatus.Equals("OK", StringComparison.OrdinalIgnoreCase) || 
                              HealthStatus.Equals("Healthy", StringComparison.OrdinalIgnoreCase);

    public bool IsHealthWarning => HealthStatus.Equals("Warning", StringComparison.OrdinalIgnoreCase) || 
                                  HealthStatus.Equals("Degraded", StringComparison.OrdinalIgnoreCase);

    public bool IsHealthError => HealthStatus.Equals("Pred Fail", StringComparison.OrdinalIgnoreCase) || 
                                HealthStatus.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                                HealthStatus.Equals("Unhealthy", StringComparison.OrdinalIgnoreCase);

    public string HealthStatusDisplay => IsHealthOk ? Strings.S.DriveHealthOk : 
                                         IsHealthWarning ? Strings.S.DriveHealthWarning : 
                                         IsHealthError ? Strings.S.DriveHealthCritical : 
                                         HealthStatus;

    public string HealthTooltip => Strings.FormatDriveHealthTooltip(HealthStatus, MediaType);

    public bool IsLowSpace => FreePercentage < 15.0 || FreeBytes < (15UL * 1024 * 1024 * 1024);
    public bool IsCriticalSpace => FreePercentage < 5.0 || FreeBytes < (5UL * 1024 * 1024 * 1024);

    public string DisplayTitle => string.IsNullOrWhiteSpace(VolumeName)
        ? DeviceId
        : $"{DeviceId} ({VolumeName})";

    public string FormattedCapacitySummary => Strings.FormatDriveCapacity(FormattedFreeSpace, FormattedTotalSize, UsedPercentage);

    public string CopyDetailsText => $"{DisplayTitle} — {FileSystem} | {FormattedCapacitySummary} | {MediaType} ({HealthStatus})";

    public static string FormatBytes(ulong bytes)
    {
        double gb = bytes / (1024.0 * 1024.0 * 1024.0);
        if (gb >= 1000.0)
        {
            double tb = gb / 1024.0;
            return $"{tb:F2} TB";
        }
        return $"{gb:F1} GB";
    }
}

/// <summary>
/// Represents the full snapshot of local storage drives and partitions for a computer.
/// </summary>
public record ComputerDiskSnapshot
{
    public string Hostname { get; init; } = string.Empty;
    public List<ComputerDiskDriveInfo> Drives { get; init; } = [];
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime QueriedAt { get; init; } = DateTime.Now;
}
