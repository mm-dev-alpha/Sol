using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service interface for querying live hardware, BIOS, and OS diagnostic information from remote endpoints.
/// </summary>
public interface IComputerDiagnosticService
{
    /// <summary>
    /// Queries the hardware, BIOS, CPU, RAM, and OS build snapshot for a remote computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the remote query.</param>
    /// <returns>A populated <see cref="ComputerHardwareSnapshot"/> object.</returns>
    Task<ComputerHardwareSnapshot> GetHardwareSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the uptime, last boot time, and pending reboot diagnostic snapshot for a remote computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the remote query.</param>
    /// <returns>A populated <see cref="ComputerUptimeSnapshot"/> object.</returns>
    Task<ComputerUptimeSnapshot> GetUptimeSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the storage drives, partitions, capacity, and health snapshot for a remote computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the remote query.</param>
    /// <returns>A populated <see cref="ComputerDiskSnapshot"/> object.</returns>
    Task<ComputerDiskSnapshot> GetDiskSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the battery health, degradation, cycle count, and power state snapshot for a computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the remote query.</param>
    /// <returns>A populated <see cref="ComputerBatterySnapshot"/> object.</returns>
    Task<ComputerBatterySnapshot> GetBatterySnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the active and disconnected interactive / RDP logon sessions on a target computer.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the remote query.</param>
    /// <returns>A populated <see cref="ComputerSessionSnapshot"/> object.</returns>
    Task<ComputerSessionSnapshot> GetSessionSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely disconnects or logs off an active session on the target computer.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="sessionId">The session ID to terminate.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    Task DisconnectSessionAsync(string targetHost, uint sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the active running processes on the target computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the remote query.</param>
    /// <returns>A populated <see cref="ComputerProcessSnapshot"/> object.</returns>
    Task<ComputerProcessSnapshot> GetProcessesSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely terminates a running process on the target computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="processId">The PID of the process to terminate.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>True if the process was successfully terminated.</returns>
    Task<bool> TerminateProcessAsync(string targetHost, uint processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers an immediate background Group Policy refresh ('gpupdate.exe /force /nowait') on the target computer.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>True if the process was successfully initiated.</returns>
    Task<bool> TriggerGroupPolicyUpdateAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the BitLocker volume encryption, protection, and conversion status on the target computer's system drive.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the remote query.</param>
    /// <returns>A populated <see cref="ComputerBitLockerSnapshot"/> object.</returns>
    Task<ComputerBitLockerSnapshot> GetBitLockerStatusAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends BitLocker drive encryption protection on the target computer for a specified number of reboots (default: 1).
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="rebootCount">The number of reboots to keep protection suspended (default: 1).</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>True if protection was successfully suspended.</returns>
    Task<bool> SuspendBitLockerProtectionAsync(string targetHost, uint rebootCount = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes BitLocker drive encryption protection on the target computer immediately.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>True if protection was successfully resumed.</returns>
    Task<bool> ResumeBitLockerProtectionAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a vendor warranty lookup URL for recognized hardware manufacturers (Dell, Lenovo, HP).
    /// </summary>
    /// <param name="manufacturer">The manufacturer name (e.g. Dell, Lenovo, HP).</param>
    /// <param name="serialNumber">The serial number or service tag.</param>
    /// <returns>A web URL string, or null if unsupported.</returns>
    string? GetWarrantyUrl(string? manufacturer, string? serialNumber);

    /// <summary>
    /// Queries the Windows services snapshot on the target computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="cancellationToken">Cancellation token to abort the remote query.</param>
    /// <returns>A populated <see cref="ComputerServicesSnapshot"/> object.</returns>
    Task<ComputerServicesSnapshot> GetServicesSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely starts a Windows service on the target computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="serviceName">The name of the service to start.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>True if the service was successfully started.</returns>
    Task<bool> StartServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely stops a Windows service on the target computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="serviceName">The name of the service to stop.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>True if the service was successfully stopped.</returns>
    Task<bool> StopServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely restarts a Windows service on the target computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="serviceName">The name of the service to restart.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>True if the service was successfully restarted.</returns>
    Task<bool> RestartServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely sets the start mode (startup type) of a Windows service on the target computer endpoint.
    /// </summary>
    /// <param name="targetHost">The DNS hostname or IP address of the target computer.</param>
    /// <param name="serviceName">The name of the service to modify.</param>
    /// <param name="startMode">The new start mode: 'Auto', 'Manual', or 'Disabled'.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>True if the start mode was successfully modified.</returns>
    Task<bool> SetServiceStartModeAsync(string targetHost, string serviceName, string startMode, CancellationToken cancellationToken = default);
}
