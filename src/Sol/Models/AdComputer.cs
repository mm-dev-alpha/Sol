using System;
using System.Collections.Generic;

namespace Sol.Models;

/// <summary>
/// Represents an Active Directory computer account object with hardware, OS, and security metadata.
/// </summary>
public record AdComputer
{
    // Identity & Network
    public string Name { get; init; } = string.Empty;
    public string SamAccountName { get; init; } = string.Empty;
    public string DnsHostName { get; init; } = string.Empty;
    public string OperatingSystem { get; init; } = string.Empty;
    public string OperatingSystemVersion { get; init; } = string.Empty;
    public string OuPath { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Sid { get; init; } = string.Empty;
    public string ManagedBy { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string IPv4Address { get; init; } = string.Empty;

    // Account Status & Activity
    public string AccountStatus { get; init; } = "Unknown";
    public bool IsEnabled { get; init; }
    public DateTime? LastLogon { get; init; }
    public DateTime? LastLogonTimestamp { get; init; }
    public DateTime? PasswordLastSet { get; init; }
    public DateTime? Created { get; init; }
    public DateTime? Modified { get; init; }

    // BitLocker Recovery Keys
    public List<BitLockerKeyInfo> BitLockerKeys { get; init; } = [];

    // Security Groups
    public List<string> Groups { get; init; } = [];
}

/// <summary>
/// Represents a BitLocker recovery key entry (msFVE-RecoveryInformation) associated with an AD computer object.
/// </summary>
public record BitLockerKeyInfo
{
    public string KeyId { get; init; } = string.Empty;
    public string RecoveryPassword { get; init; } = string.Empty;
    public DateTime Created { get; init; }
    public string FormattedCreated => Created == DateTime.MinValue ? "Unknown" : Created.ToString("g");
}