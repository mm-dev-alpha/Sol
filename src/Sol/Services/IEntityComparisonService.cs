using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service providing deterministic side-by-side diff analysis for Active Directory objects.
/// </summary>
public interface IEntityComparisonService
{
    /// <summary>
    /// Compares two Active Directory user accounts.
    /// </summary>
    UserComparisonResult CompareUsers(AdUser userA, AdUser userB);

    /// <summary>
    /// Compares two Active Directory computer accounts.
    /// </summary>
    ComputerComparisonResult CompareComputers(AdComputer computerA, AdComputer computerB);
}
