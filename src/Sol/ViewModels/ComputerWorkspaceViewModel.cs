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

    [ObservableProperty] public partial bool IsHardwareLoading { get; set; }
    [ObservableProperty] public partial bool IsUptimeLoading { get; set; }
    [ObservableProperty] public partial bool IsDiskLoading { get; set; }
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

    public bool IsDiagnosticsLoading => IsHardwareLoading || IsUptimeLoading || IsDiskLoading;

    public string GroupCountBadge => CurrentComputer?.Groups?.Count.ToString() ?? "0";
    public bool HasNoFilteredGroups => FilteredGroups.Count == 0;

    public ObservableCollection<AdComputer> SearchResults { get; } = new();
    public ObservableCollection<AdComputer> CenterSuggestions { get; } = new();
    public ObservableCollection<string> FilteredGroups { get; } = new();
    public ObservableCollection<string> GroupSearchSuggestions { get; } = new();
    public ObservableCollection<BitLockerKeyInfo> BitLockerKeys { get; } = new();
    public ObservableCollection<ComputerDiskDriveInfo> Drives { get; } = new();

    public ComputerWorkspaceViewModel(
        IActiveDirectoryService adService, 
        INavigationService navigationService,
        IComputerDiagnosticService diagnosticService)
    {
        _adService = adService;
        _navigationService = navigationService;
        _diagnosticService = diagnosticService;
    }

    [RelayCommand]
    public void ResetToHeroState()
    {
        _diagnosticCts?.Cancel();
        _diagnosticCts?.Dispose();
        _diagnosticCts = null;
        HardwareSnapshot = null;
        IsHardwareLoading = false;

        CurrentComputer = null;
        SearchResults.Clear();
        CenterSuggestions.Clear();
        FilteredGroups.Clear();
        BitLockerKeys.Clear();
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

    public async Task FetchHardwareSnapshotAsync(AdComputer computer) => await FetchDiagnosticsAsync(computer);

    public async Task FetchDiagnosticsAsync(AdComputer computer)
    {
        _diagnosticCts?.Cancel();
        _diagnosticCts?.Dispose();
        _diagnosticCts = new System.Threading.CancellationTokenSource();
        var token = _diagnosticCts.Token;

        string targetHost = !string.IsNullOrWhiteSpace(computer.DnsHostName) ? computer.DnsHostName : computer.Name;
        if (string.IsNullOrWhiteSpace(targetHost)) return;

        IsHardwareLoading = true;
        IsUptimeLoading = true;
        IsDiskLoading = true;
        HardwareSnapshot = null;
        UptimeSnapshot = null;
        DiskSnapshot = null;
        Drives.Clear();
        NotifyHardwarePropertiesChanged();
        NotifyUptimePropertiesChanged();
        NotifyDiskPropertiesChanged();

        try
        {
            var hwTask = _diagnosticService.GetHardwareSnapshotAsync(targetHost, token);
            var uptimeTask = _diagnosticService.GetUptimeSnapshotAsync(targetHost, token);
            var diskTask = _diagnosticService.GetDiskSnapshotAsync(targetHost, token);

            await Task.WhenAll(hwTask, uptimeTask, diskTask);

            if (!token.IsCancellationRequested)
            {
                HardwareSnapshot = await hwTask;
                UptimeSnapshot = await uptimeTask;
                DiskSnapshot = await diskTask;
                RefreshDrivesCollection();
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
            }
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsHardwareLoading = false;
                IsUptimeLoading = false;
                IsDiskLoading = false;
                NotifyHardwarePropertiesChanged();
                NotifyUptimePropertiesChanged();
                NotifyDiskPropertiesChanged();
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
        CurrentComputer = null;
        SearchResults.Clear();
        FilteredGroups.Clear();
        BitLockerKeys.Clear();
        HardwareSnapshot = null;
        UptimeSnapshot = null;
        DiskSnapshot = null;
        Drives.Clear();
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

    private void ShowInfo(string message)
    {
        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(message, InfoBarSeverity.Informational));
    }

    private void ShowError(string message)
    {
        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(message, InfoBarSeverity.Error));
    }
}