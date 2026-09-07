using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Aggregate facade interface combining hardware diagnostics, process & service management, and BitLocker encryption diagnostics.
/// </summary>
public interface IComputerDiagnosticService : IHardwareDiagnosticService, IProcessManagementService, IBitLockerManagementService
{
}
