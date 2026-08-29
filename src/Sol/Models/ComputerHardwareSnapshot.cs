using System;

namespace Sol.Models;

/// <summary>
/// Represents a hardware, BIOS, and OS diagnostic snapshot of a remote computer endpoint.
/// </summary>
public record ComputerHardwareSnapshot
{
    public string Hostname { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string BiosVersion { get; init; } = string.Empty;
    public string BiosReleaseDate { get; init; } = string.Empty;
    public string OsCaption { get; init; } = string.Empty;
    public string OsVersion { get; init; } = string.Empty;
    public string BuildNumber { get; init; } = string.Empty;
    public string DisplayVersion { get; init; } = string.Empty;
    public string CpuName { get; init; } = string.Empty;
    public string TotalMemoryFormatted { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime QueriedAt { get; init; } = DateTime.Now;

    public string FormattedBuild => !string.IsNullOrWhiteSpace(DisplayVersion)
        ? $"{DisplayVersion} (Build {BuildNumber})"
        : (!string.IsNullOrWhiteSpace(BuildNumber) ? $"Build {BuildNumber}" : "—");

    public bool HasWarrantySupport => !string.IsNullOrWhiteSpace(SerialNumber) &&
        (Manufacturer.Contains("Dell", StringComparison.OrdinalIgnoreCase) ||
         Manufacturer.Contains("Lenovo", StringComparison.OrdinalIgnoreCase) ||
         Manufacturer.Contains("HP", StringComparison.OrdinalIgnoreCase) ||
         Manufacturer.Contains("Hewlett", StringComparison.OrdinalIgnoreCase));
}
