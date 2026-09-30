using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service contract for querying and managing Remote Desktop Services sessions and profile disks.
/// </summary>
public interface IRdsService
{
    /// <summary>
    /// Checks if the application process is running with elevated Administrator privileges.
    /// </summary>
    bool IsElevated();

    /// <summary>
    /// Restarts the application process with Administrator elevation (UAC prompt).
    /// </summary>
    bool RestartAsAdministrator();

    /// <summary>
    /// Tests connectivity to the specified RDS Connection Broker.
    /// </summary>
    Task<bool> TestBrokerConnectionAsync(string broker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries all active and disconnected RDS user sessions from the connection broker.
    /// </summary>
    Task<IReadOnlyList<RdsSessionItem>> GetSessionsAsync(string broker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces logoff of a user session.
    /// </summary>
    Task<bool> LogoffSessionAsync(string broker, string hostServer, int unifiedSessionId, bool force = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects a user session without terminating running processes.
    /// </summary>
    Task<bool> DisconnectSessionAsync(string broker, string hostServer, int unifiedSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries session collections configured on the connection broker.
    /// </summary>
    Task<IReadOnlyList<RdsCollectionInfo>> GetCollectionsAsync(string broker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inspects a User Profile Disk (VHDX) file on a UNC share or local volume.
    /// </summary>
    Task<RdsDiskLayoutInfo> InspectUpdDiskAsync(string vhdxPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expands the virtual disk file and extends the underlying NTFS partition.
    /// </summary>
    Task<RdsDiskExpansionResult> ExpandUpdDiskAsync(
        string broker,
        string collectionName,
        string vhdxPath,
        string userSid,
        string samAccountName,
        ulong additionalGigabytes,
        IProgress<RdsExpansionStep>? progress = null,
        CancellationToken cancellationToken = default);
}
