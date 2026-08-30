using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service contract for querying JIRA tickets created by Active Directory users.
/// Supports both Jira Data Center / Server and Jira Cloud REST APIs.
/// </summary>
public interface IJiraService
{
    Task<List<JiraTicket>> GetTicketsCreatedByUserAsync(
        string userEmailOrSam, 
        int startAt = 0, 
        int maxResults = 10, 
        CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(
        string? overrideBaseUrl = null, 
        string? overrideMode = null, 
        string? overrideEmail = null, 
        string? overrideSecret = null, 
        CancellationToken cancellationToken = default);
}
