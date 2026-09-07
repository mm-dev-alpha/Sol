using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service interface for querying live hardware, BIOS, uptime, storage, and power diagnostics from computer endpoints.
/// </summary>
public interface IHardwareDiagnosticService
{
    /// <summary>
    /// Queries the hardware, BIOS, CPU, RAM, and OS build snapshot for a remote computer endpoint.
    /// </summary>
    Task<ComputerHardwareSnapshot> GetHardwareSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the uptime, last boot time, and pending reboot diagnostic snapshot for a remote computer endpoint.
    /// </summary>
    Task<ComputerUptimeSnapshot> GetUptimeSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the storage drives, partitions, capacity, and health snapshot for a remote computer endpoint.
    /// </summary>
    Task<ComputerDiskSnapshot> GetDiskSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the battery health, degradation, cycle count, and power state snapshot for a computer endpoint.
    /// </summary>
    Task<ComputerBatterySnapshot> GetBatterySnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a vendor warranty lookup URL for recognized hardware manufacturers (Dell, Lenovo, HP).
    /// </summary>
    string? GetWarrantyUrl(string? manufacturer, string? serialNumber);
}
