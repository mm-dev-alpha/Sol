using System;
using System.Collections.Generic;
using System.Linq;

namespace Sol.Models;

/// <summary>
/// Represents a Windows service on a target computer endpoint.
/// </summary>
public record ComputerServiceInfo
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string State { get; init; } = "Stopped";
    public string StartMode { get; init; } = "Manual";
    public string StartName { get; init; } = string.Empty;
    public bool AcceptStop { get; init; } = true;
    public bool AcceptPause { get; init; }
    public string PathName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public uint ProcessId { get; init; }

    /// <summary>
    /// Critical Windows OS core services that must not be stopped remotely to protect system stability.
    /// </summary>
    public static readonly HashSet<string> CriticalServiceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "RpcSs",
        "RpcEptMapper",
        "DcomLaunch",
        "EventLog",
        "PlugPlay",
        "Winmgmt",
        "CryptSvc",
        "BrokerInfrastructure",
        "LSM",
        "Power",
        "ProfSvc",
        "SamSs",
        "Schedule",
        "SystemEventsBroker",
        "Netlogon",
        "LanmanServer",
        "LanmanWorkstation",
        "NTDS",
        "gpsvc",
        "CoreMessagingRegistrar",
        "StateRepository",
        "UserManager",
        "Dhcp",
        "Dnscache"
    };

    public static bool IsCritical(string? serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return false;
        return CriticalServiceNames.Contains(serviceName);
    }

    public bool IsCriticalService => IsCritical(Name);

    public bool IsRunning => string.Equals(State, "Running", StringComparison.OrdinalIgnoreCase);
    public bool IsStopped => string.Equals(State, "Stopped", StringComparison.OrdinalIgnoreCase);
    public bool IsPending => State.Contains("Pending", StringComparison.OrdinalIgnoreCase);
    public bool IsPaused => string.Equals(State, "Paused", StringComparison.OrdinalIgnoreCase);

    public bool CanStop => !IsCriticalService && IsRunning && AcceptStop;
    public bool CanStart => IsStopped;
    public bool CanRestart => !IsCriticalService && IsRunning && AcceptStop;
    public bool CanChangeStartMode => !IsCriticalService;

    public string DisplayStartName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(StartName)) return "—";
            if (string.Equals(StartName, "LocalSystem", StringComparison.OrdinalIgnoreCase)) return "NT AUTHORITY\\SYSTEM";
            return StartName;
        }
    }

    public string NormalizedStartMode
    {
        get
        {
            if (string.Equals(StartMode, "Auto", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(StartMode, "Automatic", StringComparison.OrdinalIgnoreCase))
            {
                return "Auto";
            }
            if (string.Equals(StartMode, "Disabled", StringComparison.OrdinalIgnoreCase))
            {
                return "Disabled";
            }
            return "Manual";
        }
    }

    public int StartModeIndex
    {
        get => NormalizedStartMode switch
        {
            "Auto" => 0,
            "Manual" => 1,
            "Disabled" => 2,
            _ => 1
        };
    }

    public string StatusDisplay => IsRunning ? Helpers.Strings.S.ServiceStatusRunning : (IsStopped ? Helpers.Strings.S.ServiceStatusStopped : State);
    public string StartTooltip => Helpers.Strings.S.StartServiceTooltip;
    public string StopTooltip => Helpers.Strings.S.StopServiceTooltip;
    public string RestartTooltip => Helpers.Strings.S.RestartServiceTooltip;
    public string CriticalProtectedTooltip => Helpers.Strings.S.CriticalServiceProtected;
    public string StartModeAutoDisplay => Helpers.Strings.S.ServiceStartModeAuto;
    public string StartModeManualDisplay => Helpers.Strings.S.ServiceStartModeManual;
    public string StartModeDisabledDisplay => Helpers.Strings.S.ServiceStartModeDisabled;

    public Microsoft.UI.Xaml.Visibility RunningVisibility => IsRunning ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility StoppedVisibility => IsStopped ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility PendingVisibility => IsPending ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility CriticalVisibility => IsCriticalService ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
}

/// <summary>
/// Diagnostic snapshot containing all Windows services on a target workstation.
/// </summary>
public record ComputerServicesSnapshot
{
    public string Hostname { get; init; } = string.Empty;
    public List<ComputerServiceInfo> Services { get; init; } = [];
    public bool IsSuccess { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;

    public int TotalServiceCount => Services.Count;
    public int RunningCount => Services.Count(s => s.IsRunning);
    public int StoppedCount => Services.Count(s => s.IsStopped);
    public int OtherCount => Services.Count(s => !s.IsRunning && !s.IsStopped);
}
