using System;
using Sol.Helpers;

namespace Sol.Models;

/// <summary>
/// Represents a battery health, degradation, and power state diagnostic snapshot of a mobile endpoint or laptop.
/// </summary>
public record ComputerBatterySnapshot
{
    public string Hostname { get; init; } = string.Empty;
    public bool HasBattery { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public string Chemistry { get; init; } = string.Empty;
    public uint DesignCapacityMWh { get; init; }
    public uint FullChargeCapacityMWh { get; init; }
    public uint EstimatedChargeRemainingPercent { get; init; }
    public string BatteryStatusText { get; init; } = string.Empty;
    public bool IsCharging { get; init; }
    public bool IsAcConnected { get; init; }
    public int? CycleCount { get; init; }
    public TimeSpan? EstimatedRunTime { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime QueriedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// Battery health percentage: (FullChargeCapacity / DesignCapacity) * 100, clamped at 100.0%.
    /// </summary>
    public double HealthPercentage => (DesignCapacityMWh > 0 && FullChargeCapacityMWh > 0)
        ? Math.Min(100.0, Math.Max(0.0, Math.Round((double)FullChargeCapacityMWh / DesignCapacityMWh * 100.0, 1)))
        : 100.0;

    /// <summary>
    /// Wear percentage: 100 - HealthPercentage.
    /// </summary>
    public double WearPercentage => Math.Max(0.0, Math.Round(100.0 - HealthPercentage, 1));

    public bool IsHealthOk => IsSuccess && HasBattery && HealthPercentage >= 80.0;
    public bool IsHealthWarning => IsSuccess && HasBattery && HealthPercentage >= 50.0 && HealthPercentage < 80.0;
    public bool IsHealthCritical => IsSuccess && HasBattery && HealthPercentage < 50.0;

    public string HealthStatusDisplay => IsHealthOk
        ? string.Format(Strings.S.BatteryHealthOkFormat, HealthPercentage)
        : (IsHealthWarning
            ? string.Format(Strings.S.BatteryHealthWarningFormat, HealthPercentage)
            : string.Format(Strings.S.BatteryHealthCriticalFormat, HealthPercentage));

    public string FormattedDesignCapacity => DesignCapacityMWh >= 1000
        ? $"{DesignCapacityMWh / 1000.0:F1} Wh"
        : $"{DesignCapacityMWh} mWh";

    public string FormattedFullChargeCapacity => FullChargeCapacityMWh >= 1000
        ? $"{FullChargeCapacityMWh / 1000.0:F1} Wh"
        : $"{FullChargeCapacityMWh} mWh";

    public string FormattedCapacitySummary => (DesignCapacityMWh > 0 && FullChargeCapacityMWh > 0)
        ? $"{FormattedFullChargeCapacity} / {FormattedDesignCapacity}"
        : "—";

    public string FormattedWearNotice => $"{Strings.S.BatteryWearNotice}: {WearPercentage:F1}%";

    public string FormattedCycleCount => CycleCount.HasValue
        ? Strings.FormatBatteryCycles(CycleCount.Value)
        : Strings.S.BatteryCyclesUnknown;

    public string FormattedEstimatedRunTime => EstimatedRunTime.HasValue && EstimatedRunTime.Value > TimeSpan.Zero
        ? Strings.FormatDuration(EstimatedRunTime.Value)
        : Strings.S.BatteryRuntimeUnknown;

    public string FormattedChargeSummary => $"{EstimatedChargeRemainingPercent}%";

    public string CopyDetailsText =>
        $"{Strings.S.BatteryAndPowerTitle}: {Hostname}\n" +
        $"{Strings.S.BatteryHealthLabel}: {HealthStatusDisplay} ({Strings.S.BatteryWearNotice}: {WearPercentage:F1}%)\n" +
        $"{Strings.S.BatteryChargeRemainingLabel}: {EstimatedChargeRemainingPercent}% ({BatteryStatusText})\n" +
        $"{Strings.S.BatteryFullChargeCapacityLabel}: {FormattedFullChargeCapacity}\n" +
        $"{Strings.S.BatteryDesignCapacityLabel}: {FormattedDesignCapacity}\n" +
        $"{Strings.S.BatteryCycleCountLabel}: {FormattedCycleCount}\n" +
        $"{Strings.S.BatteryRuntimeLabel}: {FormattedEstimatedRunTime}\n" +
        (!string.IsNullOrWhiteSpace(Chemistry) ? $"Chemistry: {Chemistry}\n" : "");
}
