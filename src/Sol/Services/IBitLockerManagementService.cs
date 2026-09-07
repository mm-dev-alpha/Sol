using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service interface for querying and managing BitLocker encryption status and protection on computer endpoints.
/// </summary>
public interface IBitLockerManagementService
{
    /// <summary>
    /// Queries the BitLocker volume encryption, protection, and conversion status on the target computer's system drive.
    /// </summary>
    Task<ComputerBitLockerSnapshot> GetBitLockerStatusAsync(string targetHost, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends BitLocker drive encryption protection on the target computer for a specified number of reboots.
    /// </summary>
    Task<bool> SuspendBitLockerProtectionAsync(string targetHost, uint rebootCount = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes BitLocker drive encryption protection on the target computer immediately.
    /// </summary>
    Task<bool> ResumeBitLockerProtectionAsync(string targetHost, CancellationToken cancellationToken = default);
}
