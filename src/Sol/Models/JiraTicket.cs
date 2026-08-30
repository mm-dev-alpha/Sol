using System;

namespace Sol.Models;

/// <summary>
/// Represents a JIRA issue / ticket created by an Active Directory user.
/// </summary>
public record JiraTicket
{
    public string Key { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusCategoryKey { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string PriorityIconUrl { get; init; } = string.Empty;
    public DateTime Created { get; init; } = DateTime.MinValue;
    public string BrowseUrl { get; init; } = string.Empty;

    public string FormattedCreated => Created == DateTime.MinValue ? "—" : Created.ToString("g");

    /// <summary>
    /// Checks whether the issue is resolved or done.
    /// </summary>
    public bool IsDone => string.Equals(StatusCategoryKey, "done", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(Status, "Done", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(Status, "Closed", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(Status, "Resolved", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether the issue is currently in progress.
    /// </summary>
    public bool IsInProgress => string.Equals(StatusCategoryKey, "indeterminate", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(Status, "In Progress", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(Status, "In Review", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(Status, "Testing", StringComparison.OrdinalIgnoreCase);
}
