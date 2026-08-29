using System.Collections.Generic;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

public interface IActiveDirectoryService
{
    // Search
    Task<List<AdUser>> SearchUsersAsync(string query);
    Task<List<string>> SearchGroupsAsync(string query);

    // Read
    Task<List<KeyValuePair<string, string>>> GetAllUserAttributesAsync(string samAccountName);

    // Write - Account State
    Task UnlockAccountAsync(string samAccountName);
    Task EnableAccountAsync(string samAccountName, bool enable);
    Task ResetPasswordAsync(string samAccountName, string newPassword, bool requireChangeAtNextLogon);
    Task ForcePasswordChangeAsync(string samAccountName);

    // Write - Profile & Groups
    Task UpdateUserProfileAsync(string samAccountName, Dictionary<string, string> attributes, string? newManager);
    Task UpdateRawAttributeAsync(string samAccountName, string attributeName, string newValue);
    Task AddUserToGroupAsync(string samAccountName, string groupName);
    Task RemoveUserFromGroupAsync(string samAccountName, string groupName);

    // Computer Operations
    Task<List<AdComputer>> SearchComputersAsync(string query);
    Task EnableComputerAccountAsync(string samAccountName, bool enable);
    Task AddComputerToGroupAsync(string samAccountName, string groupName);
    Task RemoveComputerFromGroupAsync(string samAccountName, string groupName);
}
