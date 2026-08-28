namespace Sol.Models;

public record AdUser
{
    // Identity
    public string GivenName { get; init; } = string.Empty;
    public string Surname { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string SamAccountName { get; init; } = string.Empty;
    public string Upn { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Sid { get; init; } = string.Empty;
    public string EmployeeId { get; init; } = string.Empty;
    public string OuPath { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string WebPage { get; init; } = string.Empty;

    // Organization
    public string Department { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Manager { get; init; } = string.Empty;
    public List<string> DirectReports { get; init; } = [];

    // Contact
    public string Office { get; init; } = string.Empty;
    public string OfficePhone { get; init; } = string.Empty;
    public string MobilePhone { get; init; } = string.Empty;
    public string StreetAddress { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;

    // Account Status
    public string AccountStatus { get; init; } = "Unknown";
    public DateTime? AccountExpires { get; init; }
    public string AccountExpiresStatus { get; init; } = string.Empty;
    public bool IsLockedOut { get; init; }
    public bool PasswordNeverExpires { get; init; }

    // Password & Logon
    public DateTime? LastLogon { get; init; }
    public DateTime? LastLogonTimestamp { get; init; }
    public DateTime? PasswordExpiry { get; init; }
    public DateTime? PasswordLastSet { get; init; }
    public string PasswordExpiryStatus { get; init; } = string.Empty;
    public int BadPasswordCount { get; init; }
    public DateTime? BadPasswordTime { get; init; }

    // Groups
    public List<string> Groups { get; init; } = [];
}

/// <summary>
/// Represents a raw Active Directory attribute key-value pair for inspection and safe editing.
/// </summary>
public record AdAttributeItem(string Key, string Value);

