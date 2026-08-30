using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Models;
using Sol.Services;
using Sol.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sol.ViewModels;

public partial class ComputerWorkspaceViewModel : ObservableObject
{
    private readonly IActiveDirectoryService _adService;
    private readonly INavigationService _navigationService;
    private readonly IComputerDiagnosticService _diagnosticService;
    private System.Threading.CancellationTokenSource? _diagnosticCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComputerContentVisibility))]
    [NotifyPropertyChangedFor(nameof(EmptyStateVisibility))]
    [NotifyPropertyChangedFor(nameof(MultipleMatchesVisibility))]
    [NotifyPropertyChangedFor(nameof(IsAccountEnabled))]
    [NotifyPropertyChangedFor(nameof(IsAccountDisabled))]
    [NotifyPropertyChangedFor(nameof(FormattedPasswordLastSet))]
    [NotifyPropertyChangedFor(nameof(FormattedLastLogon))]
    [NotifyPropertyChangedFor(nameof(FormattedCreated))]
    [NotifyPropertyChangedFor(nameof(HasBitLockerKeys))]
    [NotifyPropertyChangedFor(nameof(BitLockerKeysCount))]
    [NotifyPropertyChangedFor(nameof(HasManagedBy))]
    public partial AdComputer? CurrentComputer { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHardwareSnapshot))]
    [NotifyPropertyChangedFor(nameof(HasHardwareError))]
    [NotifyPropertyChangedFor(nameof(HasWarrantyLink))]
    [NotifyPropertyChangedFor(nameof(WarrantyUrl))]
    public partial ComputerHardwareSnapshot? HardwareSnapshot { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUptimeSnapshot))]
    [NotifyPropertyChangedFor(nameof(HasUptimeError))]
    public partial ComputerUptimeSnapshot? UptimeSnapshot { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDiskSnapshot))]
    [NotifyPropertyChangedFor(nameof(HasDiskError))]
    [NotifyPropertyChangedFor(nameof(HasNoDrivesFound))]
    [NotifyPropertyChangedFor(nameof(DrivesCountBadge))]
    public partial ComputerDiskSnapshot? DiskSnapshot { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBatterySnapshot))]
    [NotifyPropertyChangedFor(nameof(HasBatteryError))]
    [NotifyPropertyChangedFor(nameof(IsDesktopOrNoBattery))]
    [NotifyPropertyChangedFor(nameof(BatterySectionVisibility))]
    public partial ComputerBatterySnapshot? BatterySnapshot { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSessionSnapshot))]
    [NotifyPropertyChangedFor(nameof(HasSessionError))]
    [NotifyPropertyChangedFor(nameof(HasNoActiveSessions))]
    [NotifyPropertyChangedFor(nameof(ActiveSessionsCountBadge))]
    public partial ComputerSessionSnapshot? SessionSnapshot { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProcessSnapshot))]
    [NotifyPropertyChangedFor(nameof(HasProcessError))]
    [NotifyPropertyChangedFor(nameof(TotalProcessCount))]
    [NotifyPropertyChangedFor(nameof(TotalProcessesCountBadge))]
    public partial ComputerProcessSnapshot? ProcessSnapshot { get; set; }

    [ObservableProperty] public partial bool IsHardwareLoading { get; set; }
    [ObservableProperty] public partial bool IsUptimeLoading { get; set; }
    [ObservableProperty] public partial bool IsDiskLoading { get; set; }
    [ObservableProperty] public partial bool IsBatteryLoading { get; set; }
    [ObservableProperty] public partial bool IsSessionsLoading { get; set; }
    [ObservableProperty] public partial bool IsProcessesLoading { get; set; }
    [ObservableProperty] public partial string ProcessFilterQuery { get; set; } = string.Empty;
    [ObservableProperty] public partial string ProcessSortOption { get; set; } = "Memory";
    [ObservableProperty] public partial string ProcessSortColumn { get; set; } = "Memory";
    [ObservableProperty] public partial bool ProcessSortAscending { get; set; } = false;
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;
    [ObservableProperty] public partial string GroupFilterQuery { get; set; } = string.Empty;
    [ObservableProperty] public partial string CenterSearchQuery { get; set; } = string.Empty;
    [ObservableProperty] public partial string NewGroupName { get; set; } = string.Empty;

    public Visibility ComputerContentVisibility => CurrentComputer != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyStateVisibility => CurrentComputer == null && SearchResults.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MultipleMatchesVisibility => SearchResults.Count > 0 && CurrentComputer == null ? Visibility.Visible : Visibility.Collapsed;

    public bool IsAccountEnabled => CurrentComputer?.IsEnabled == true;
    public bool IsAccountDisabled => CurrentComputer?.IsEnabled == false;
    public string FormattedPasswordLastSet => CurrentComputer?.PasswordLastSet?.ToString("g") ?? "N/A";
    public string FormattedLastLogon => CurrentComputer?.LastLogon?.ToString("g") ?? (CurrentComputer?.LastLogonTimestamp?.ToString("g") ?? "N/A");
    public string FormattedCreated => CurrentComputer?.Created?.ToString("g") ?? "N/A";
    public string FormattedModified => CurrentComputer?.Modified?.ToString("g") ?? "N/A";
    public bool HasBitLockerKeys => CurrentComputer?.BitLockerKeys?.Count > 0;
    public string BitLockerKeysCount => CurrentComputer?.BitLockerKeys?.Count.ToString() ?? "0";
    public bool HasManagedBy => !string.IsNullOrWhiteSpace(CurrentComputer?.ManagedBy);

    public bool HasHardwareSnapshot => HardwareSnapshot != null && HardwareSnapshot.IsSuccess;
    public bool HasHardwareError => HardwareSnapshot != null && !HardwareSnapshot.IsSuccess;
    public bool HasWarrantyLink => !string.IsNullOrWhiteSpace(WarrantyUrl);
    public string? WarrantyUrl => _diagnosticService.GetWarrantyUrl(HardwareSnapshot?.Manufacturer, HardwareSnapshot?.SerialNumber);

    public bool HasUptimeSnapshot => UptimeSnapshot != null && UptimeSnapshot.IsSuccess;
    public bool HasUptimeError => UptimeSnapshot != null && !UptimeSnapshot.IsSuccess;

    public bool HasDiskSnapshot => DiskSnapshot != null && DiskSnapshot.IsSuccess && DiskSnapshot.Drives.Count > 0;
    public bool HasDiskError => DiskSnapshot != null && !DiskSnapshot.IsSuccess;
    public bool HasNoDrivesFound => DiskSnapshot != null && DiskSnapshot.IsSuccess && DiskSnapshot.Drives.Count == 0;
    public string DrivesCountBadge => DiskSnapshot?.Drives?.Count.ToString() ?? "0";

    public bool HasBatterySnapshot => BatterySnapshot != null && BatterySnapshot.IsSuccess && BatterySnapshot.HasBattery;
    public bool HasBatteryError => BatterySnapshot != null && !BatterySnapshot.IsSuccess;
    public bool IsDesktopOrNoBattery => BatterySnapshot != null && BatterySnapshot.IsSuccess && !BatterySnapshot.HasBattery;
    public Visibility BatterySectionVisibility => (HasBatterySnapshot || IsBatteryLoading || HasBatteryError) ? Visibility.Visible : Visibility.Collapsed;

    public bool HasSessionSnapshot => SessionSnapshot != null && SessionSnapshot.IsSuccess && SessionSnapshot.Sessions.Count > 0;
    public bool HasSessionError => SessionSnapshot != null && !SessionSnapshot.IsSuccess;
    public bool HasNoActiveSessions => SessionSnapshot != null && SessionSnapshot.IsSuccess && SessionSnapshot.Sessions.Count == 0;
    public string ActiveSessionsCountBadge => SessionSnapshot?.Sessions?.Count.ToString() ?? "0";

    public bool HasProcessSnapshot => ProcessSnapshot != null && ProcessSnapshot.IsSuccess;
    public bool HasProcessError => ProcessSnapshot != null && !ProcessSnapshot.IsSuccess;
    public int TotalProcessCount => ProcessSnapshot?.Processes?.Count ?? 0;
    public string TotalProcessesCountBadge => Strings.TotalProcessesCountBadge(TotalProcessCount);

    public bool IsDiagnosticsLoading => IsHardwareLoading || IsUptimeLoading || IsDiskLoading || IsBatteryLoading || IsSessionsLoading;

    public string GroupCountBadge => CurrentComputer?.Groups?.Count.ToString() ?? "0";
    public bool HasNoFilteredGroups => FilteredGroups.Count == 0;

    public ObservableCollection<AdComputer> SearchResults { get; } = new();
    public ObservableCollection<AdComputer> CenterSuggestions { get; } = new();
    public ObservableCollection<string> FilteredGroups { get; } = new();
    public ObservableCollection<string> GroupSearchSuggestions { get; } = new();
    public ObservableCollection<BitLockerKeyInfo> BitLockerKeys { get; } = new();
    public ObservableCollection<ComputerDiskDriveInfo> Drives { get; } = new();
    public ObservableCollection<ComputerSessionInfo> Sessions { get; } = new();
    public ObservableCollection<ComputerProcessInfo> FilteredProcesses { get; } = new();

    public ComputerWorkspaceViewModel(
        IActiveDirectoryService adService, 
        INavigationService navigationService,
        IComputerDiagnosticService diagnosticService)
    {
        _adService = adService;
        _navigationService = navigationService;
        _diagnosticService = diagnosticService;
    }

    public event Action? CloseProcessManagerRequested;

    public void RequestCloseProcessManager()
    {
        CloseProcessManagerRequested?.Invoke();
    }

    [RelayCommand]
    public void ResetToHeroState()
    {
        RequestCloseProcessManager();
        _diagnosticCts?.Cancel();
        _diagnosticCts = null;
        HardwareSnapshot = null;
        IsHardwareLoading = false;
        SessionSnapshot = null;
        IsSessionsLoading = false;
        ProcessSnapshot = null;
        IsProcessesLoading = false;

        CurrentComputer = null;
        SearchResults.Clear();
        CenterSuggestions.Clear();
        FilteredGroups.Clear();
        BitLockerKeys.Clear();
        Sessions.Clear();
        FilteredProcesses.Clear();
        CenterSearchQuery = string.Empty;
        GroupFilterQuery = string.Empty;
        NewGroupName = string.Empty;
        NotifyPropertiesChanged();
    }

    [RelayCommand]
    public async Task SearchCenterAsync(string query)
    {
        CenterSearchQuery = query;
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            CenterSuggestions.Clear();
            return;
        }

        try
        {
            var results = await _adService.SearchComputersAsync(query);
            CenterSuggestions.Clear();
            foreach (var c in results) CenterSuggestions.Add(c);
        }
        catch
        {
            // Silent ignore suggestion failure
        }
    }

    [RelayCommand]
    public async Task SearchAndLoadComputerAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        IsLoading = true;
        SearchResults.Clear();
        CurrentComputer = null;

        try
        {
            var results = await _adService.SearchComputersAsync(query);
            if (results.Count == 1)
            {
                await LoadComputerAsync(results[0]);
            }
            else if (results.Count > 1)
            {
                foreach (var c in results) SearchResults.Add(c);
                NotifyPropertiesChanged();
            }
            else
            {
                ShowError(Strings.S.NoComputerFound);
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadComputerAsync(AdComputer computer)
    {
        RequestCloseProcessManager();
        IsLoading = true;
        SearchResults.Clear();
        try
        {
            CurrentComputer = computer;
            RefreshFilteredGroups();
            RefreshBitLockerKeys();
            NotifyPropertiesChanged();
            _ = FetchHardwareSnapshotAsync(computer);
        }
        finally
        {
            IsLoading = false;
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    public void FilterGroups(string query)
    {
        GroupFilterQuery = query;
        RefreshFilteredGroups();
    }

    public void RefreshFilteredGroups()
    {
        var groups = CurrentComputer?.Groups ?? new List<string>();
        var query = GroupFilterQuery;
        var filtered = string.IsNullOrWhiteSpace(query)
            ? groups
            : groups.Where(g => g.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        FilteredGroups.Clear();
        foreach (var g in filtered)
        {
            FilteredGroups.Add(g);
        }
        OnPropertyChanged(nameof(HasNoFilteredGroups));
    }

    [RelayCommand]
    public async Task SearchGroupsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            GroupSearchSuggestions.Clear();
            return;
        }

        try
        {
            var results = await _adService.SearchGroupsAsync(query);
            GroupSearchSuggestions.Clear();
            foreach (var r in results) GroupSearchSuggestions.Add(r);
        }
        catch
        {
            // Ignore suggestions failure
        }
    }

    public void RefreshBitLockerKeys()
    {
        BitLockerKeys.Clear();
        if (CurrentComputer?.BitLockerKeys != null)
        {
            foreach (var key in CurrentComputer.BitLockerKeys)
            {
                BitLockerKeys.Add(key);
            }
        }
    }

    [RelayCommand]
    public async Task ToggleComputerAccountAsync()
    {
        if (CurrentComputer == null) return;
        bool targetState = !CurrentComputer.IsEnabled;
        IsLoading = true;
        try
        {
            await _adService.EnableComputerAccountAsync(CurrentComputer.SamAccountName, targetState);
            CurrentComputer = CurrentComputer with
            {
                IsEnabled = targetState,
                AccountStatus = targetState ? "Enabled" : "Disabled"
            };
            NotifyPropertiesChanged();
            ShowInfo(targetState ? Strings.S.ComputerEnabledSuccess : Strings.S.ComputerDisabledSuccess);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddToGroupAsync(string groupName)
    {
        if (CurrentComputer == null || string.IsNullOrWhiteSpace(groupName)) return;
        IsLoading = true;
        try
        {
            await _adService.AddComputerToGroupAsync(CurrentComputer.SamAccountName, groupName);
            ShowInfo(Strings.AddedToGroupSuccess(groupName));
            NewGroupName = string.Empty;

            var updatedGroups = new List<string>(CurrentComputer.Groups) { groupName };
            CurrentComputer = CurrentComputer with { Groups = updatedGroups };
            RefreshFilteredGroups();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RemoveFromGroupAsync(string groupName)
    {
        if (CurrentComputer == null || string.IsNullOrWhiteSpace(groupName)) return;
        IsLoading = true;
        try
        {
            await _adService.RemoveComputerFromGroupAsync(CurrentComputer.SamAccountName, groupName);
            ShowInfo(Strings.RemovedFromGroupSuccess(groupName));

            var updatedGroups = CurrentComputer.Groups.Where(g => !string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase)).ToList();
            CurrentComputer = CurrentComputer with { Groups = updatedGroups };
            RefreshFilteredGroups();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void CopyBitLockerKey(string recoveryPassword)
    {
        if (string.IsNullOrWhiteSpace(recoveryPassword)) return;
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(recoveryPassword);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        ShowInfo(Strings.S.BitLockerKeyCopiedSuccess);
    }

    [RelayCommand]
    public void CopyPowerShellCommand(string cmdType)
    {
        if (CurrentComputer == null) return;
        string targetHost = !string.IsNullOrWhiteSpace(CurrentComputer.DnsHostName) ? CurrentComputer.DnsHostName : CurrentComputer.Name;

        string script = cmdType switch
        {
            "Get-ADComputer" => $"Get-ADComputer -Identity \"{CurrentComputer.SamAccountName}\" -Properties *",
            "Test-Connection" => $"Test-Connection -TargetName \"{targetHost}\" -Count 4",
            "Enter-PSSession" => $"Enter-PSSession -ComputerName \"{targetHost}\"",
            "mstsc" => $"mstsc /v:{targetHost}",
            _ => $"Get-ADComputer -Identity \"{CurrentComputer.SamAccountName}\""
        };

        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(script);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        ShowInfo(Strings.S.PowerShellCopiedSuccess);
    }

    [RelayCommand]
    public void CopyAllDetails()
    {
        if (CurrentComputer == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Name: {CurrentComputer.Name}");
        sb.AppendLine($"SamAccountName: {CurrentComputer.SamAccountName}");
        sb.AppendLine($"DNS Host Name: {CurrentComputer.DnsHostName}");
        sb.AppendLine($"Operating System: {CurrentComputer.OperatingSystem} {CurrentComputer.OperatingSystemVersion}");
        sb.AppendLine($"OU Path: {CurrentComputer.OuPath}");
        sb.AppendLine($"Status: {CurrentComputer.AccountStatus}");
        sb.AppendLine($"Managed By: {CurrentComputer.ManagedBy}");
        sb.AppendLine($"Location: {CurrentComputer.Location}");
        sb.AppendLine($"Password Last Set: {FormattedPasswordLastSet}");
        sb.AppendLine($"Last Logon: {FormattedLastLogon}");
        sb.AppendLine($"Groups: {string.Join(", ", CurrentComputer.Groups)}");
        if (CurrentComputer.BitLockerKeys.Count > 0)
        {
            sb.AppendLine("BitLocker Keys:");
            foreach (var key in CurrentComputer.BitLockerKeys)
            {
                sb.AppendLine($"  - ID: {key.KeyId} | Password: {key.RecoveryPassword} | Created: {key.FormattedCreated}");
            }
        }

        if (HardwareSnapshot != null && HardwareSnapshot.IsSuccess)
        {
            sb.AppendLine("Hardware Diagnostics:");
            if (!string.IsNullOrWhiteSpace(HardwareSnapshot.Manufacturer) || !string.IsNullOrWhiteSpace(HardwareSnapshot.Model))
                sb.AppendLine($"  - Model: {HardwareSnapshot.Manufacturer} {HardwareSnapshot.Model}".Trim());
            if (!string.IsNullOrWhiteSpace(HardwareSnapshot.SerialNumber))
                sb.AppendLine($"  - Serial / Service Tag: {HardwareSnapshot.SerialNumber}");
            if (!string.IsNullOrWhiteSpace(HardwareSnapshot.BiosVersion))
                sb.AppendLine($"  - BIOS: {HardwareSnapshot.BiosVersion} ({HardwareSnapshot.BiosReleaseDate})");
            if (!string.IsNullOrWhiteSpace(HardwareSnapshot.BuildNumber))
                sb.AppendLine($"  - OS Build: {HardwareSnapshot.FormattedBuild}");
            if (!string.IsNullOrWhiteSpace(HardwareSnapshot.CpuName))
                sb.AppendLine($"  - CPU: {HardwareSnapshot.CpuName}");
            if (!string.IsNullOrWhiteSpace(HardwareSnapshot.TotalMemoryFormatted))
                sb.AppendLine($"  - Memory: {HardwareSnapshot.TotalMemoryFormatted}");
        }

        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(sb.ToString());
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        ShowInfo(Strings.S.AllInfoCopiedSuccess);
    }

    [RelayCommand]
    public async Task RefreshHardwareSnapshotAsync()
    {
        if (CurrentComputer == null) return;
        await FetchHardwareSnapshotAsync(CurrentComputer);
    }

    [RelayCommand]
    public async Task RefreshBatterySnapshotAsync()
    {
        if (CurrentComputer == null) return;
        string targetHost = !string.IsNullOrWhiteSpace(CurrentComputer.DnsHostName) ? CurrentComputer.DnsHostName : CurrentComputer.Name;
        if (string.IsNullOrWhiteSpace(targetHost)) return;

        IsBatteryLoading = true;
        NotifyBatteryPropertiesChanged();

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
            BatterySnapshot = await _diagnosticService.GetBatterySnapshotAsync(targetHost, cts.Token);
        }
        catch (Exception ex)
        {
            BatterySnapshot = new ComputerBatterySnapshot
            {
                Hostname = targetHost,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            IsBatteryLoading = false;
            NotifyBatteryPropertiesChanged();
        }
    }

    [RelayCommand]
    public async Task RefreshSessionsSnapshotAsync()
    {
        if (CurrentComputer == null) return;
        IsSessionsLoading = true;
        NotifySessionPropertiesChanged();

        string targetHost = !string.IsNullOrWhiteSpace(CurrentComputer.DnsHostName) ? CurrentComputer.DnsHostName : CurrentComputer.Name;
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            IsSessionsLoading = false;
            NotifySessionPropertiesChanged();
            return;
        }

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
            SessionSnapshot = await _diagnosticService.GetSessionSnapshotAsync(targetHost, cts.Token);
            RefreshSessionsCollection();
        }
        catch (Exception ex)
        {
            SessionSnapshot = new ComputerSessionSnapshot
            {
                Hostname = targetHost,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            IsSessionsLoading = false;
            NotifySessionPropertiesChanged();
        }
    }

    [RelayCommand]
    public async Task DisconnectSessionAsync(ComputerSessionInfo? session)
    {
        if (CurrentComputer == null || session == null || !session.SessionId.HasValue) return;
        try
        {
            string targetHost = !string.IsNullOrWhiteSpace(CurrentComputer.DnsHostName) ? CurrentComputer.DnsHostName : CurrentComputer.Name;
            await _diagnosticService.DisconnectSessionAsync(targetHost, session.SessionId.Value);
            ShowInfo(Strings.DisconnectSuccess(session.EffectiveDisplayName));
            await RefreshSessionsSnapshotAsync();
        }
        catch (Exception ex)
        {
            ShowError(Strings.DisconnectFailed(session.EffectiveDisplayName, ex.Message));
        }
    }

    [RelayCommand]
    public async Task NavigateToSessionUserAsync(string? samAccountName)
    {
        if (string.IsNullOrWhiteSpace(samAccountName)) return;
        var userVm = App.GetService<UserWorkspaceViewModel>();
        await userVm.LoadUserAsync(samAccountName);
        _navigationService.NavigateTo("UserWorkspacePage");
    }

    [RelayCommand]
    public async Task OpenVendorWarrantyAsync()
    {
        if (string.IsNullOrWhiteSpace(WarrantyUrl)) return;
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(WarrantyUrl));
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    public async Task RefreshProcessesAsync()
    {
        if (CurrentComputer == null) return;
        IsProcessesLoading = true;

        string targetHost = !string.IsNullOrWhiteSpace(CurrentComputer.DnsHostName) ? CurrentComputer.DnsHostName : CurrentComputer.Name;
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            IsProcessesLoading = false;
            return;
        }

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
            ProcessSnapshot = await _diagnosticService.GetProcessesSnapshotAsync(targetHost, cts.Token);
            ApplyProcessFilterAndSort();
        }
        catch (Exception ex)
        {
            ProcessSnapshot = new ComputerProcessSnapshot
            {
                Hostname = targetHost,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
            FilteredProcesses.Clear();
        }
        finally
        {
            IsProcessesLoading = false;
        }
    }

    [RelayCommand]
    public async Task TerminateProcessAsync(ComputerProcessInfo? process)
    {
        if (CurrentComputer == null || process == null) return;
        if (!process.CanTerminate)
        {
            ShowError(Strings.S.CriticalProcessCannotBeTerminated);
            return;
        }

        try
        {
            string targetHost = !string.IsNullOrWhiteSpace(CurrentComputer.DnsHostName) ? CurrentComputer.DnsHostName : CurrentComputer.Name;
            bool success = await _diagnosticService.TerminateProcessAsync(targetHost, process.ProcessId);
            if (success)
            {
                ShowInfo(Strings.ProcessTerminatedSuccess(process.Name));
                await RefreshProcessesAsync();
            }
            else
            {
                ShowError(Strings.TerminateProcessFailedNamed(process.Name, process.ProcessId));
            }
        }
        catch (Exception ex)
        {
            ShowError(Strings.TerminateProcessFailed(ex.Message));
        }
    }

    [RelayCommand]
    public void FilterProcesses(string? query)
    {
        ProcessFilterQuery = query ?? string.Empty;
        ApplyProcessFilterAndSort();
    }

    [RelayCommand]
    public void SortProcesses(string? sortOption)
    {
        if (!string.IsNullOrWhiteSpace(sortOption))
        {
            ProcessSortOption = sortOption;
            ProcessSortColumn = sortOption;
            ApplyProcessFilterAndSort();
        }
    }

    [RelayCommand]
    public void ToggleProcessSort(string? column)
    {
        if (string.IsNullOrWhiteSpace(column)) return;

        if (string.Equals(ProcessSortColumn, column, StringComparison.OrdinalIgnoreCase))
        {
            ProcessSortAscending = !ProcessSortAscending;
        }
        else
        {
            ProcessSortColumn = column;
            ProcessSortOption = column;
            // Numbers / metrics default to descending (high to low), text/ID default to ascending
            ProcessSortAscending = column switch
            {
                "Name" or "User" or "PID" => true,
                _ => false
            };
        }

        ApplyProcessFilterAndSort();
    }

    public void ApplyProcessFilterAndSort()
    {
        FilteredProcesses.Clear();
        if (ProcessSnapshot?.Processes == null) return;

        IEnumerable<ComputerProcessInfo> query = ProcessSnapshot.Processes;

        if (!string.IsNullOrWhiteSpace(ProcessFilterQuery))
        {
            string q = ProcessFilterQuery.Trim();
            query = query.Where(p =>
                p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.ProcessId.ToString().Contains(q) ||
                p.DisplayOwner.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        query = ProcessSortColumn switch
        {
            "PID" => ProcessSortAscending ? query.OrderBy(p => p.ProcessId) : query.OrderByDescending(p => p.ProcessId),
            "Name" => ProcessSortAscending ? query.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase) : query.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase),
            "User" => ProcessSortAscending ? query.OrderBy(p => p.DisplayOwner, StringComparer.OrdinalIgnoreCase) : query.OrderByDescending(p => p.DisplayOwner, StringComparer.OrdinalIgnoreCase),
            "CPU" => ProcessSortAscending ? query.OrderBy(p => p.CpuUsagePercent) : query.OrderByDescending(p => p.CpuUsagePercent),
            "Network" => ProcessSortAscending ? query.OrderBy(p => p.NetworkMbps) : query.OrderByDescending(p => p.NetworkMbps),
            _ => ProcessSortAscending ? query.OrderBy(p => p.WorkingSetBytes) : query.OrderByDescending(p => p.WorkingSetBytes)
        };

        foreach (var proc in query)
        {
            FilteredProcesses.Add(proc);
        }
    }

    public async Task FetchHardwareSnapshotAsync(AdComputer computer) => await FetchDiagnosticsAsync(computer);

    public async Task FetchDiagnosticsAsync(AdComputer computer)
    {
        _diagnosticCts?.Cancel();
        _diagnosticCts = new System.Threading.CancellationTokenSource();
        var token = _diagnosticCts.Token;

        string targetHost = !string.IsNullOrWhiteSpace(computer.DnsHostName) ? computer.DnsHostName : computer.Name;
        if (string.IsNullOrWhiteSpace(targetHost)) return;

        IsHardwareLoading = true;
        IsUptimeLoading = true;
        IsDiskLoading = true;
        IsBatteryLoading = true;
        IsSessionsLoading = true;
        HardwareSnapshot = null;
        UptimeSnapshot = null;
        DiskSnapshot = null;
        BatterySnapshot = null;
        SessionSnapshot = null;
        Drives.Clear();
        Sessions.Clear();
        NotifyHardwarePropertiesChanged();
        NotifyUptimePropertiesChanged();
        NotifyDiskPropertiesChanged();
        NotifyBatteryPropertiesChanged();
        NotifySessionPropertiesChanged();

        try
        {
            var hwTask = _diagnosticService.GetHardwareSnapshotAsync(targetHost, token);
            var uptimeTask = _diagnosticService.GetUptimeSnapshotAsync(targetHost, token);
            var diskTask = _diagnosticService.GetDiskSnapshotAsync(targetHost, token);
            var batteryTask = _diagnosticService.GetBatterySnapshotAsync(targetHost, token);
            var sessionTask = _diagnosticService.GetSessionSnapshotAsync(targetHost, token);

            await Task.WhenAll(hwTask, uptimeTask, diskTask, batteryTask, sessionTask);

            if (!token.IsCancellationRequested)
            {
                HardwareSnapshot = await hwTask;
                UptimeSnapshot = await uptimeTask;
                DiskSnapshot = await diskTask;
                BatterySnapshot = await batteryTask;
                SessionSnapshot = await sessionTask;
                RefreshDrivesCollection();
                RefreshSessionsCollection();
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                HardwareSnapshot ??= new ComputerHardwareSnapshot
                {
                    Hostname = targetHost,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
                UptimeSnapshot ??= new ComputerUptimeSnapshot
                {
                    Hostname = targetHost,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
                DiskSnapshot ??= new ComputerDiskSnapshot
                {
                    Hostname = targetHost,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
                BatterySnapshot ??= new ComputerBatterySnapshot
                {
                    Hostname = targetHost,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
                SessionSnapshot ??= new ComputerSessionSnapshot
                {
                    Hostname = targetHost,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsHardwareLoading = false;
                IsUptimeLoading = false;
                IsDiskLoading = false;
                IsBatteryLoading = false;
                IsSessionsLoading = false;
                NotifyHardwarePropertiesChanged();
                NotifyUptimePropertiesChanged();
                NotifyDiskPropertiesChanged();
                NotifyBatteryPropertiesChanged();
                NotifySessionPropertiesChanged();
            }
        }
    }

    private void RefreshDrivesCollection()
    {
        Drives.Clear();
        if (DiskSnapshot?.Drives != null)
        {
            foreach (var drive in DiskSnapshot.Drives)
            {
                Drives.Add(drive);
            }
        }
    }

    private void RefreshSessionsCollection()
    {
        Sessions.Clear();
        if (SessionSnapshot?.Sessions != null)
        {
            foreach (var session in SessionSnapshot.Sessions)
            {
                Sessions.Add(session);
            }
        }
    }

    [RelayCommand]
    public void CopyToClipboard(object? parameter)
    {
        string? text = parameter?.ToString();
        if (string.IsNullOrEmpty(text)) return;
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(text);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        ShowInfo(Strings.S.CopiedToClipboard);
    }

    [RelayCommand]
    public async Task NavigateToManagedByUserAsync()
    {
        if (CurrentComputer == null || string.IsNullOrWhiteSpace(CurrentComputer.ManagedBy)) return;
        var userVm = App.GetService<UserWorkspaceViewModel>();
        await userVm.LoadUserAsync(CurrentComputer.ManagedBy);
        _navigationService.NavigateTo("UserWorkspacePage");
    }

    [RelayCommand]
    public void CloseWorkspace()
    {
        RequestCloseProcessManager();
        CurrentComputer = null;
        SearchResults.Clear();
        FilteredGroups.Clear();
        BitLockerKeys.Clear();
        HardwareSnapshot = null;
        UptimeSnapshot = null;
        DiskSnapshot = null;
        BatterySnapshot = null;
        SessionSnapshot = null;
        ProcessSnapshot = null;
        Drives.Clear();
        Sessions.Clear();
        FilteredProcesses.Clear();
        CenterSearchQuery = string.Empty;
        NotifyPropertiesChanged();
    }

    public void NotifyPropertiesChanged()
    {
        OnPropertyChanged(nameof(CurrentComputer));
        OnPropertyChanged(nameof(ComputerContentVisibility));
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(MultipleMatchesVisibility));
        OnPropertyChanged(nameof(IsAccountEnabled));
        OnPropertyChanged(nameof(IsAccountDisabled));
        OnPropertyChanged(nameof(FormattedPasswordLastSet));
        OnPropertyChanged(nameof(FormattedLastLogon));
        OnPropertyChanged(nameof(FormattedCreated));
        OnPropertyChanged(nameof(FormattedModified));
        OnPropertyChanged(nameof(HasBitLockerKeys));
        OnPropertyChanged(nameof(BitLockerKeysCount));
        OnPropertyChanged(nameof(HasManagedBy));
        OnPropertyChanged(nameof(GroupCountBadge));
        OnPropertyChanged(nameof(HasNoFilteredGroups));
        NotifyHardwarePropertiesChanged();
        NotifyUptimePropertiesChanged();
        NotifyDiskPropertiesChanged();
        NotifyBatteryPropertiesChanged();
        NotifySessionPropertiesChanged();
    }

    public void NotifyHardwarePropertiesChanged()
    {
        OnPropertyChanged(nameof(HardwareSnapshot));
        OnPropertyChanged(nameof(IsHardwareLoading));
        OnPropertyChanged(nameof(IsDiagnosticsLoading));
        OnPropertyChanged(nameof(HasHardwareSnapshot));
        OnPropertyChanged(nameof(HasHardwareError));
        OnPropertyChanged(nameof(HasWarrantyLink));
        OnPropertyChanged(nameof(WarrantyUrl));
    }

    public void NotifyUptimePropertiesChanged()
    {
        OnPropertyChanged(nameof(UptimeSnapshot));
        OnPropertyChanged(nameof(IsUptimeLoading));
        OnPropertyChanged(nameof(IsDiagnosticsLoading));
        OnPropertyChanged(nameof(HasUptimeSnapshot));
        OnPropertyChanged(nameof(HasUptimeError));
    }

    public void NotifyDiskPropertiesChanged()
    {
        OnPropertyChanged(nameof(DiskSnapshot));
        OnPropertyChanged(nameof(IsDiskLoading));
        OnPropertyChanged(nameof(IsDiagnosticsLoading));
        OnPropertyChanged(nameof(HasDiskSnapshot));
        OnPropertyChanged(nameof(HasDiskError));
        OnPropertyChanged(nameof(HasNoDrivesFound));
        OnPropertyChanged(nameof(DrivesCountBadge));
    }

    public void NotifyBatteryPropertiesChanged()
    {
        OnPropertyChanged(nameof(BatterySnapshot));
        OnPropertyChanged(nameof(IsBatteryLoading));
        OnPropertyChanged(nameof(IsDiagnosticsLoading));
        OnPropertyChanged(nameof(HasBatterySnapshot));
        OnPropertyChanged(nameof(HasBatteryError));
        OnPropertyChanged(nameof(IsDesktopOrNoBattery));
        OnPropertyChanged(nameof(BatterySectionVisibility));
    }

    public void NotifySessionPropertiesChanged()
    {
        OnPropertyChanged(nameof(SessionSnapshot));
        OnPropertyChanged(nameof(IsSessionsLoading));
        OnPropertyChanged(nameof(IsDiagnosticsLoading));
        OnPropertyChanged(nameof(HasSessionSnapshot));
        OnPropertyChanged(nameof(HasSessionError));
        OnPropertyChanged(nameof(HasNoActiveSessions));
        OnPropertyChanged(nameof(ActiveSessionsCountBadge));
    }

    public void ShowInfo(string message)
    {
        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(message, InfoBarSeverity.Informational));
    }

    public void ShowError(string message)
    {
        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(message, InfoBarSeverity.Error));
    }
}