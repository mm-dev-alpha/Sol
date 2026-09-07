using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service interface for querying and managing processes, sessions, services, and policies on computer endpoints.
/// </summary>
public interface IProcessManagementService
{
    /// <summary>
    /// Queries the active running processes on the target computer endpoint.
    /// </summary>
    Task<ComputerProcessSnapshot> GetProcessesSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely terminates a running process on the target computer endpoint.
    /// </summary>
    Task<bool> TerminateProcessAsync(string targetHost, uint processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the active and disconnected interactive / RDP logon sessions on a target computer.
    /// </summary>
    Task<ComputerSessionSnapshot> GetSessionSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely disconnects or logs off an active session on the target computer.
    /// </summary>
    Task DisconnectSessionAsync(string targetHost, uint sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers an immediate background Group Policy refresh on the target computer.
    /// </summary>
    Task<bool> TriggerGroupPolicyUpdateAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the Windows services snapshot on the target computer endpoint.
    /// </summary>
    Task<ComputerServicesSnapshot> GetServicesSnapshotAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely starts a Windows service on the target computer endpoint.
    /// </summary>
    Task<bool> StartServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely stops a Windows service on the target computer endpoint.
    /// </summary>
    Task<bool> StopServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely restarts a Windows service on the target computer endpoint.
    /// </summary>
    Task<bool> RestartServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remotely sets the start mode (startup type) of a Windows service on the target computer endpoint.
    /// </summary>
    Task<bool> SetServiceStartModeAsync(string targetHost, string serviceName, string startMode, CancellationToken cancellationToken = default);
}
