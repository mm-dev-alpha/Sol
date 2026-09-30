using System;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Sol.Models;

/// <summary>
/// Represents an active or disconnected user session retrieved from an RDS Connection Broker.
/// </summary>
public sealed class RdsSessionItem
{
    public string Username { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public string State { get; set; } = string.Empty;
    public string HostServer { get; set; } = string.Empty;
    public int UnifiedSessionId { get; set; }
    public DateTime? LogonTime { get; set; }
    public string FormattedLogonTime { get; set; } = string.Empty;

    public bool IsActive => string.Equals(State, "STATE_ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(State, "Active", StringComparison.OrdinalIgnoreCase);

    public bool IsDisconnected => string.Equals(State, "STATE_DISCONNECTED", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(State, "Disconnected", StringComparison.OrdinalIgnoreCase);

    public string UserPrincipalDisplay => string.IsNullOrWhiteSpace(Domain)
        ? Username
        : $"{Domain}\\{Username}";
}

/// <summary>
/// Represents an RDS session collection configured on the Connection Broker.
/// </summary>
public sealed class RdsCollectionInfo
{
    public string CollectionName { get; set; } = string.Empty;
    public bool UpdEnabled { get; set; }
    public string UpdDiskPath { get; set; } = string.Empty;
}

/// <summary>
/// Detailed inspection data of a User Profile Disk (UPD) VHDX file.
/// </summary>
public sealed class RdsDiskLayoutInfo
{
    public string VhdxPath { get; set; } = string.Empty;
    public ulong CapacityBytes { get; set; }
    public string PartitionStyle { get; set; } = "GPT"; // GPT or MBR
    public int PartitionNumber { get; set; } = 1;
    public ulong PartitionSizeBytes { get; set; }
    public bool IsFileLocked { get; set; }
    public bool Exists { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public double CapacityGiB => Math.Round(CapacityBytes / (1024.0 * 1024.0 * 1024.0), 2);
    public double PartitionSizeGiB => Math.Round(PartitionSizeBytes / (1024.0 * 1024.0 * 1024.0), 2);
}

/// <summary>
/// Result report of an RDS User Profile Disk expansion operation.
/// </summary>
public sealed class RdsDiskExpansionResult
{
    public bool IsSuccess { get; set; }
    public ulong InitialCapacityBytes { get; set; }
    public ulong TargetCapacityBytes { get; set; }
    public ulong FinalCapacityBytes { get; set; }
    public ulong FinalPartitionBytes { get; set; }
    public string? ErrorMessage { get; set; }

    public double FinalCapacityGiB => Math.Round(FinalCapacityBytes / (1024.0 * 1024.0 * 1024.0), 2);
    public double FinalPartitionGiB => Math.Round(FinalPartitionBytes / (1024.0 * 1024.0 * 1024.0), 2);
}

/// <summary>
/// Multi-step progression phases during a User Profile Disk resize.
/// </summary>
public enum RdsExpansionStep
{
    CheckingPrerequisites = 0,
    VerifyingSessionsAndLock = 1,
    ExpandingVirtualDisk = 2,
    ResizingNtfsPartition = 3,
    VerifyingAndCompleting = 4
}

/// <summary>
/// Broadcast message when RDS workspace settings change.
/// </summary>
public sealed class RdsSettingsChangedMessage : ValueChangedMessage<bool>
{
    public RdsSettingsChangedMessage(bool isEnabled) : base(isEnabled)
    {
    }

    public bool IsEnabled => Value;
}
