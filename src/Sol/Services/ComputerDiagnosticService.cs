using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Aggregate facade service implementing <see cref="IComputerDiagnosticService"/> by coordinating
/// <see cref="IHardwareDiagnosticService"/>, <see cref="IProcessManagementService"/>, and <see cref="IBitLockerManagementService"/>.
/// </summary>
public class ComputerDiagnosticService : IComputerDiagnosticService
{
    private readonly IHardwareDiagnosticService _hardwareDiagnosticService;
    private readonly IProcessManagementService _processManagementService;
    private readonly IBitLockerManagementService _bitLockerManagementService;

    /// <summary>
    /// Initializes a new instance of <see cref="ComputerDiagnosticService"/> with default subservices.
    /// </summary>
    public ComputerDiagnosticService()
        : this(new HardwareDiagnosticService(), new ProcessManagementService(), new BitLockerManagementService())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ComputerDiagnosticService"/> with injected subservices.
    /// </summary>
    public ComputerDiagnosticService(
        IHardwareDiagnosticService hardwareDiagnosticService,
        IProcessManagementService processManagementService,
        IBitLockerManagementService bitLockerManagementService)
    {
        _hardwareDiagnosticService = hardwareDiagnosticService ?? throw new ArgumentNullException(nameof(hardwareDiagnosticService));
        _processManagementService = processManagementService ?? throw new ArgumentNullException(nameof(processManagementService));
        _bitLockerManagementService = bitLockerManagementService ?? throw new ArgumentNullException(nameof(bitLockerManagementService));
    }

    #region IHardwareDiagnosticService Delegation

    /// <inheritdoc />
    public Task<ComputerHardwareSnapshot> GetHardwareSnapshotAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _hardwareDiagnosticService.GetHardwareSnapshotAsync(targetHost, cancellationToken);

    /// <inheritdoc />
    public Task<ComputerUptimeSnapshot> GetUptimeSnapshotAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _hardwareDiagnosticService.GetUptimeSnapshotAsync(targetHost, cancellationToken);

    /// <inheritdoc />
    public Task<ComputerDiskSnapshot> GetDiskSnapshotAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _hardwareDiagnosticService.GetDiskSnapshotAsync(targetHost, cancellationToken);

    /// <inheritdoc />
    public Task<ComputerBatterySnapshot> GetBatterySnapshotAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _hardwareDiagnosticService.GetBatterySnapshotAsync(targetHost, cancellationToken);

    /// <inheritdoc />
    public string? GetWarrantyUrl(string? manufacturer, string? serialNumber) =>
        _hardwareDiagnosticService.GetWarrantyUrl(manufacturer, serialNumber);

    #endregion

    #region IProcessManagementService Delegation

    /// <inheritdoc />
    public Task<ComputerProcessSnapshot> GetProcessesSnapshotAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _processManagementService.GetProcessesSnapshotAsync(targetHost, cancellationToken);

    /// <inheritdoc />
    public Task<bool> TerminateProcessAsync(string targetHost, uint processId, CancellationToken cancellationToken = default) =>
        _processManagementService.TerminateProcessAsync(targetHost, processId, cancellationToken);

    /// <inheritdoc />
    public Task<ComputerSessionSnapshot> GetSessionSnapshotAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _processManagementService.GetSessionSnapshotAsync(targetHost, cancellationToken);

    /// <inheritdoc />
    public Task DisconnectSessionAsync(string targetHost, uint sessionId, CancellationToken cancellationToken = default) =>
        _processManagementService.DisconnectSessionAsync(targetHost, sessionId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> TriggerGroupPolicyUpdateAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _processManagementService.TriggerGroupPolicyUpdateAsync(targetHost, cancellationToken);

    /// <inheritdoc />
    public Task<ComputerServicesSnapshot> GetServicesSnapshotAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _processManagementService.GetServicesSnapshotAsync(targetHost, cancellationToken);

    /// <inheritdoc />
    public Task<bool> StartServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default) =>
        _processManagementService.StartServiceAsync(targetHost, serviceName, cancellationToken);

    /// <inheritdoc />
    public Task<bool> StopServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default) =>
        _processManagementService.StopServiceAsync(targetHost, serviceName, cancellationToken);

    /// <inheritdoc />
    public Task<bool> RestartServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default) =>
        _processManagementService.RestartServiceAsync(targetHost, serviceName, cancellationToken);

    /// <inheritdoc />
    public Task<bool> SetServiceStartModeAsync(string targetHost, string serviceName, string startMode, CancellationToken cancellationToken = default) =>
        _processManagementService.SetServiceStartModeAsync(targetHost, serviceName, startMode, cancellationToken);

    #endregion

    #region IBitLockerManagementService Delegation

    /// <inheritdoc />
    public Task<ComputerBitLockerSnapshot> GetBitLockerStatusAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _bitLockerManagementService.GetBitLockerStatusAsync(targetHost, cancellationToken);

    /// <inheritdoc />
    public Task<bool> SuspendBitLockerProtectionAsync(string targetHost, uint rebootCount = 1, CancellationToken cancellationToken = default) =>
        _bitLockerManagementService.SuspendBitLockerProtectionAsync(targetHost, rebootCount, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ResumeBitLockerProtectionAsync(string targetHost, CancellationToken cancellationToken = default) =>
        _bitLockerManagementService.ResumeBitLockerProtectionAsync(targetHost, cancellationToken);

    #endregion

    #region Static Helper Forwarders for Backward Compatibility

    /// <summary>
    /// Checks whether the target host string represents the local machine.
    /// </summary>
    public static bool IsLocalHost(string host) => DiagnosticScopeHelper.IsLocalHost(host);

    /// <summary>
    /// Checks whether the target host matches simulated demo fixture names.
    /// </summary>
    public static bool IsDemoFixture(string host) => ComputerDiagnosticFixtures.IsDemoFixture(host);

    /// <summary>
    /// Queries active and disconnected interactive sessions on the local machine using native Win32 WTS APIs.
    /// </summary>
    public static List<ComputerSessionInfo> QueryLocalSessionsWts() => ProcessManagementService.QueryLocalSessionsWts();

    /// <summary>
    /// Runs a Service Control Manager (sc.exe) command against the target computer endpoint.
    /// </summary>
    public static Task<(int ExitCode, string StdOut, string StdErr)> RunScCommandAsync(
        string cleanHost,
        IReadOnlyList<string> commandArgs,
        int timeoutMs = 8000,
        CancellationToken cancellationToken = default) =>
        ProcessManagementService.RunScCommandAsync(cleanHost, commandArgs, timeoutMs, cancellationToken);

    /// <summary>
    /// Resolves the Windows display version friendly name (e.g., 23H2, 24H2) from the OS build number string.
    /// </summary>
    public static string GetWindowsDisplayVersionFromBuild(string buildNumber) =>
        HardwareDiagnosticService.GetWindowsDisplayVersionFromBuild(buildNumber);

    /// <summary>
    /// Parses a WMI / CIM datetime formatted string into a nullable <see cref="DateTime"/>.
    /// </summary>
    public static DateTime? ParseCimDateTime(string? raw) =>
        HardwareDiagnosticService.ParseCimDateTime(raw);

    #endregion
}
