using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;

namespace Sol.ViewModels;

public partial class RdsWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly IRdsService _rdsService;
    private readonly IActiveDirectoryService _adService;
    private readonly ISettingsService _settings;
    private readonly INavigationService _navigationService;

    private CancellationTokenSource? _searchCts;
    private readonly System.Timers.Timer _filterDebounceTimer;

    // --- Navigation & Header ---
    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsElevated { get; set; }

    public string ConnectionBroker => _settings.RdsConnectionBroker ?? string.Empty;
    public bool HasBroker => !string.IsNullOrWhiteSpace(ConnectionBroker);

    public Visibility ElevationWarningVisibility => IsElevated ? Visibility.Collapsed : Visibility.Visible;
    public Visibility MissingBrokerWarningVisibility => HasBroker ? Visibility.Collapsed : Visibility.Visible;

    // --- Sessions Sub-tab Properties ---
    public ObservableCollection<RdsSessionItem> AllSessions { get; } = new();
    public ObservableCollection<RdsSessionItem> FilteredSessions { get; } = new();

    [ObservableProperty]
    public partial string SessionFilterQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoadingSessions { get; set; }

    [ObservableProperty]
    public partial bool HasSessionsError { get; set; }

    [ObservableProperty]
    public partial string SessionsErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial RdsSessionItem? SelectedSession { get; set; }

    [ObservableProperty]
    public partial string SessionSortColumn { get; set; } = "LogonTime";

    [ObservableProperty]
    public partial bool SessionSortAscending { get; set; } = false;

    public int TotalSessionsCount => AllSessions.Count;
    public int ActiveSessionsCount => AllSessions.Count(s => s.IsActive);
    public int ConnectedSessionsCount => AllSessions.Count(s => s.IsConnected);
    public int DisconnectedSessionsCount => AllSessions.Count(s => s.IsDisconnected);

    public string TotalSessionsBadge => string.Format(Strings.S.RdsSessionsTotalBadgeFormat, TotalSessionsCount);
    public string ActiveSessionsBadge => string.Format(Strings.S.RdsSessionsActiveBadgeFormat, ActiveSessionsCount);
    public string ConnectedSessionsBadge => string.Format(Strings.S.RdsSessionsConnectedBadgeFormat, ConnectedSessionsCount);
    public Visibility ConnectedSessionsBadgeVisibility => ConnectedSessionsCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    public string DisconnectedSessionsBadge => string.Format(Strings.S.RdsSessionsDisconnectedBadgeFormat, DisconnectedSessionsCount);

    public bool HasNoSessions => !IsLoadingSessions && FilteredSessions.Count == 0 && !HasSessionsError;

    // --- User Profile Disks (UPD) Sub-tab Properties ---
    [ObservableProperty]
    public partial string UserSearchQuery { get; set; } = string.Empty;

    public ObservableCollection<AdUser> UserSuggestions { get; } = new();

    [ObservableProperty]
    public partial AdUser? SelectedUser { get; set; }

    public ObservableCollection<RdsCollectionInfo> Collections { get; } = new();

    [ObservableProperty]
    public partial RdsCollectionInfo? SelectedCollection { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingCollections { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingCollectionConfig { get; set; }

    [ObservableProperty]
    public partial bool HasCollectionsError { get; set; }

    [ObservableProperty]
    public partial string CollectionsErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial RdsDiskLayoutInfo? DiskLayout { get; set; }

    [ObservableProperty]
    public partial bool IsInspectingDisk { get; set; }

    public ObservableCollection<RdsSessionItem> ConflictingSessions { get; } = new();

    public bool HasSelectedUser => SelectedUser != null;
    public bool HasDisk => DiskLayout != null && DiskLayout.Exists;
    public bool IsDiskMissing => DiskLayout != null && !DiskLayout.Exists;
    public bool HasConflictingSessions => ConflictingSessions.Count > 0;

    [ObservableProperty]
    public partial double AdditionalGiB { get; set; } = 1;

    public double TargetGiB => DiskLayout != null ? Math.Round(DiskLayout.CapacityGiB + AdditionalGiB, 2) : AdditionalGiB;

    public bool CanExpand => HasSelectedUser && HasDisk && !HasConflictingSessions && !(DiskLayout?.IsFileLocked ?? true) && IsElevated && !IsExpanding;

    // --- Stepped Expansion Progress Properties ---
    [ObservableProperty]
    public partial bool IsExpanding { get; set; }

    [ObservableProperty]
    public partial RdsExpansionStep CurrentExpansionStep { get; set; } = RdsExpansionStep.CheckingPrerequisites;

    [ObservableProperty]
    public partial string ExpansionStepDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial RdsDiskExpansionResult? ExpansionResult { get; set; }

    public RdsWorkspaceViewModel(
        IRdsService rdsService,
        IActiveDirectoryService adService,
        ISettingsService settings,
        INavigationService navigationService)
    {
        _rdsService = rdsService;
        _adService = adService;
        _settings = settings;
        _navigationService = navigationService;

        IsElevated = _rdsService.IsElevated();
        AdditionalGiB = _settings.RdsDefaultDiskIncreaseGB > 0 ? _settings.RdsDefaultDiskIncreaseGB : 1;

        _filterDebounceTimer = new System.Timers.Timer(200) { AutoReset = false };
        _filterDebounceTimer.Elapsed += (s, e) =>
        {
            var dq = App.MainWindow?.DispatcherQueue;
            if (dq != null)
            {
                dq.TryEnqueue(() => ApplySessionFilter());
            }
            else
            {
                ApplySessionFilter();
            }
        };
    }

    public async Task InitializeAsync()
    {
        IsElevated = _rdsService.IsElevated();
        OnPropertyChanged(nameof(ConnectionBroker));
        OnPropertyChanged(nameof(HasBroker));
        OnPropertyChanged(nameof(ElevationWarningVisibility));
        OnPropertyChanged(nameof(MissingBrokerWarningVisibility));

        if (HasBroker)
        {
            await RefreshSessionsAsync();
            await LoadCollectionsAsync();
        }
    }

    // --- Elevation & Navigation Actions ---
    [RelayCommand]
    public void RestartAsAdministrator()
    {
        _rdsService.RestartAsAdministrator();
    }

    [RelayCommand]
    public void OpenSettings()
    {
        _navigationService.NavigateTo("SettingsPage");
    }

    // --- Sessions Sub-tab Commands ---
    [RelayCommand]
    public async Task RefreshSessionsAsync()
    {
        if (!HasBroker || IsLoadingSessions) return;

        IsLoadingSessions = true;
        HasSessionsError = false;
        SessionsErrorMessage = string.Empty;

        try
        {
            var sessions = await _rdsService.GetSessionsAsync(ConnectionBroker);
            AllSessions.Clear();
            foreach (var s in sessions)
            {
                AllSessions.Add(s);
            }
            ApplySessionFilter();
            NotifySessionsMetrics();
        }
        catch (Exception ex)
        {
            HasSessionsError = true;
            SessionsErrorMessage = ex.Message;
            string notifyText = ex.Message.StartsWith(Strings.S.RdsConnectionFailedPrompt, StringComparison.OrdinalIgnoreCase)
                ? ex.Message
                : $"{Strings.S.RdsConnectionFailedPrompt} {ex.Message}";
            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage(notifyText, InfoBarSeverity.Error));
        }
        finally
        {
            IsLoadingSessions = false;
            OnPropertyChanged(nameof(HasNoSessions));
        }
    }

    partial void OnSessionFilterQueryChanged(string value)
    {
        _filterDebounceTimer.Stop();
        _filterDebounceTimer.Start();
    }

    [RelayCommand]
    public void ToggleSessionSort(string column)
    {
        if (string.Equals(SessionSortColumn, column, StringComparison.OrdinalIgnoreCase))
        {
            SessionSortAscending = !SessionSortAscending;
        }
        else
        {
            SessionSortColumn = column;
            SessionSortAscending = true;
        }

        ApplySessionFilter();
    }

    public void ApplySessionFilter()
    {
        FilteredSessions.Clear();
        string q = (SessionFilterQuery ?? string.Empty).Trim();

        var matches = string.IsNullOrWhiteSpace(q)
            ? (IEnumerable<RdsSessionItem>)AllSessions
            : AllSessions.Where(s =>
                s.Username.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.Domain.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.HostServer.Contains(q, StringComparison.OrdinalIgnoreCase));

        IEnumerable<RdsSessionItem> sorted = (SessionSortColumn ?? string.Empty).ToLowerInvariant() switch
        {
            "username" => SessionSortAscending ? matches.OrderBy(s => s.Username) : matches.OrderByDescending(s => s.Username),
            "state" => SessionSortAscending ? matches.OrderBy(s => s.State) : matches.OrderByDescending(s => s.State),
            "hostserver" => SessionSortAscending ? matches.OrderBy(s => s.HostServer) : matches.OrderByDescending(s => s.HostServer),
            "sessionid" => SessionSortAscending ? matches.OrderBy(s => s.SessionId) : matches.OrderByDescending(s => s.SessionId),
            "logontime" => SessionSortAscending ? matches.OrderBy(s => s.LogonTime ?? DateTime.MinValue) : matches.OrderByDescending(s => s.LogonTime ?? DateTime.MinValue),
            _ => SessionSortAscending ? matches.OrderBy(s => s.LogonTime ?? DateTime.MinValue) : matches.OrderByDescending(s => s.LogonTime ?? DateTime.MinValue)
        };

        foreach (var item in sorted)
        {
            FilteredSessions.Add(item);
        }

        OnPropertyChanged(nameof(HasNoSessions));
    }

    public async Task<bool> LogoffSessionsAsync(IReadOnlyList<RdsSessionItem> sessions)
    {
        if (sessions.Count == 0) return false;

        bool anyFailed = false;
        List<string> errors = new();

        foreach (var s in sessions)
        {
            try
            {
                await _rdsService.LogoffSessionAsync(ConnectionBroker, s.HostServer, s.UnifiedSessionId, force: true);
            }
            catch (Exception ex)
            {
                anyFailed = true;
                errors.Add($"{s.Username} ({s.HostServer}): {ex.Message}");
            }
        }

        await RefreshSessionsAsync();

        if (anyFailed)
        {
            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage(string.Join(Environment.NewLine, errors), InfoBarSeverity.Error));
            return false;
        }

        WeakReferenceMessenger.Default.Send(
            new AppNotificationMessage(Strings.S.RdsSessionsLogoffSuccess, InfoBarSeverity.Success));
        return true;
    }

    public async Task<bool> DisconnectSessionsAsync(IReadOnlyList<RdsSessionItem> sessions)
    {
        if (sessions.Count == 0) return false;

        bool anyFailed = false;
        List<string> errors = new();

        foreach (var s in sessions)
        {
            try
            {
                await _rdsService.DisconnectSessionAsync(ConnectionBroker, s.HostServer, s.UnifiedSessionId);
            }
            catch (Exception ex)
            {
                anyFailed = true;
                errors.Add($"{s.Username} ({s.HostServer}): {ex.Message}");
            }
        }

        await RefreshSessionsAsync();

        if (anyFailed)
        {
            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage(string.Join(Environment.NewLine, errors), InfoBarSeverity.Error));
            return false;
        }

        WeakReferenceMessenger.Default.Send(
            new AppNotificationMessage(Strings.S.RdsSessionsDisconnectSuccess, InfoBarSeverity.Success));
        return true;
    }

    private void NotifySessionsMetrics()
    {
        OnPropertyChanged(nameof(TotalSessionsCount));
        OnPropertyChanged(nameof(ActiveSessionsCount));
        OnPropertyChanged(nameof(ConnectedSessionsCount));
        OnPropertyChanged(nameof(DisconnectedSessionsCount));
        OnPropertyChanged(nameof(TotalSessionsBadge));
        OnPropertyChanged(nameof(ActiveSessionsBadge));
        OnPropertyChanged(nameof(ConnectedSessionsBadge));
        OnPropertyChanged(nameof(ConnectedSessionsBadgeVisibility));
        OnPropertyChanged(nameof(DisconnectedSessionsBadge));
    }

    // --- Profile Disks (UPD) Sub-tab Commands ---
    public async Task UpdateUserSearchSuggestionsAsync(string query)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            UserSuggestions.Clear();
            return;
        }

        try
        {
            await Task.Delay(200, token);
            var results = await _adService.SearchUsersAsync(query);
            if (!token.IsCancellationRequested)
            {
                UserSuggestions.Clear();
                foreach (var user in results.Take(8))
                {
                    UserSuggestions.Add(user);
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task SelectUserAsync(AdUser user)
    {
        SelectedUser = user;
        UserSearchQuery = user.DisplayName;
        UserSuggestions.Clear();
        OnPropertyChanged(nameof(HasSelectedUser));

        await InspectSelectedUserProfileDiskAsync();
    }

    public async Task SearchAndSelectFirstUserAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        try
        {
            var results = await _adService.SearchUsersAsync(query.Trim());
            if (results.Count > 0)
            {
                await SelectUserAsync(results[0]);
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"RdsWorkspaceViewModel.SearchAndSelectFirstUserAsync error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadCollectionsAsync()
    {
        if (!HasBroker || IsLoadingCollections) return;

        IsLoadingCollections = true;
        HasCollectionsError = false;
        CollectionsErrorMessage = string.Empty;

        // Step 1: Prepopulate from active sessions in memory if available
        var sessionCollections = AllSessions
            .Select(s => s.CollectionName)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sessionCollections.Count > 0 && Collections.Count == 0)
        {
            foreach (var name in sessionCollections)
            {
                Collections.Add(new RdsCollectionInfo { CollectionName = name });
            }
            if (SelectedCollection == null)
            {
                SelectedCollection = Collections[0];
            }
        }

        try
        {
            var colls = await _rdsService.GetCollectionsAsync(ConnectionBroker);

            // Merge newly discovered collections
            foreach (var c in colls)
            {
                if (!Collections.Any(existing => string.Equals(existing.CollectionName, c.CollectionName, StringComparison.OrdinalIgnoreCase)))
                {
                    Collections.Add(c);
                }
            }

            if (Collections.Count > 0 && SelectedCollection == null)
            {
                SelectedCollection = Collections[0];
            }
        }
        catch (Exception ex)
        {
            if (Collections.Count == 0)
            {
                HasCollectionsError = true;
                CollectionsErrorMessage = ex.Message;
            }
            AppLog.Write($"RdsWorkspaceViewModel.LoadCollectionsAsync error: {ex.Message}");
        }
        finally
        {
            IsLoadingCollections = false;
        }
    }

    partial void OnSelectedCollectionChanged(RdsCollectionInfo? value)
    {
        if (value != null && string.IsNullOrEmpty(value.UpdDiskPath) && !value.UpdEnabled)
        {
            _ = LoadSelectedCollectionConfigAsync(value);
        }
        else
        {
            _ = InspectSelectedUserProfileDiskAsync();
        }
    }

    public async Task LoadSelectedCollectionConfigAsync(RdsCollectionInfo collection)
    {
        if (collection == null || !HasBroker) return;

        IsLoadingCollectionConfig = true;
        try
        {
            var cfg = await _rdsService.GetCollectionConfigurationAsync(ConnectionBroker, collection.CollectionName);
            collection.UpdEnabled = cfg.UpdEnabled;
            collection.UpdDiskPath = cfg.UpdDiskPath;
        }
        catch (Exception ex)
        {
            AppLog.Write($"RdsWorkspaceViewModel.LoadSelectedCollectionConfigAsync error: {ex.Message}");
        }
        finally
        {
            IsLoadingCollectionConfig = false;
            OnPropertyChanged(nameof(SelectedCollection));
            await InspectSelectedUserProfileDiskAsync();
        }
    }

    partial void OnAdditionalGiBChanged(double value)
    {
        OnPropertyChanged(nameof(TargetGiB));
        OnPropertyChanged(nameof(CanExpand));
    }

    public async Task InspectSelectedUserProfileDiskAsync()
    {
        if (SelectedUser == null || SelectedCollection == null || string.IsNullOrWhiteSpace(SelectedCollection.UpdDiskPath))
        {
            DiskLayout = null;
            ConflictingSessions.Clear();
            NotifyUpdProperties();
            return;
        }

        IsInspectingDisk = true;
        try
        {
            // Resolve user SID
            string sid = SelectedUser.Sid;
            if (string.IsNullOrWhiteSpace(sid))
            {
                try
                {
                    var account = new NTAccount(SelectedUser.SamAccountName);
                    var sidObj = account.Translate(typeof(SecurityIdentifier));
                    sid = sidObj.Value;
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(sid))
            {
                DiskLayout = new RdsDiskLayoutInfo
                {
                    ErrorMessage = "Could not resolve Security Identifier (SID) for the selected user."
                };
                return;
            }

            string vhdxPath = Path.Combine(SelectedCollection.UpdDiskPath, $"UVHD-{sid}.vhdx");
            DiskLayout = await _rdsService.InspectUpdDiskAsync(vhdxPath);

            // Check for conflicting sessions of this user on the RDS farm
            ConflictingSessions.Clear();
            var matchingSessions = AllSessions.Where(s =>
                string.Equals(s.Username, SelectedUser.SamAccountName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.Username, SelectedUser.Upn, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var ms in matchingSessions)
            {
                ConflictingSessions.Add(ms);
            }
        }
        catch (Exception ex)
        {
            DiskLayout = new RdsDiskLayoutInfo
            {
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            IsInspectingDisk = false;
            NotifyUpdProperties();
        }
    }

    [RelayCommand]
    public async Task LogoffConflictingSessionsAsync()
    {
        if (ConflictingSessions.Count == 0) return;

        var toLogoff = ConflictingSessions.ToList();
        bool success = await LogoffSessionsAsync(toLogoff);
        if (success)
        {
            await Task.Delay(1500); // Allow server profile unload
            await InspectSelectedUserProfileDiskAsync();
        }
    }

    [RelayCommand]
    public async Task ExpandDiskAsync()
    {
        if (!CanExpand || DiskLayout == null || SelectedUser == null || SelectedCollection == null)
            return;

        IsExpanding = true;
        ExpansionResult = null;
        CurrentExpansionStep = RdsExpansionStep.CheckingPrerequisites;
        ExpansionStepDescription = Strings.S.RdsUpdStepChecking;
        OnPropertyChanged(nameof(CanExpand));

        var progress = new Progress<RdsExpansionStep>(step =>
        {
            CurrentExpansionStep = step;
            ExpansionStepDescription = step switch
            {
                RdsExpansionStep.CheckingPrerequisites => Strings.S.RdsUpdStepChecking,
                RdsExpansionStep.VerifyingSessionsAndLock => Strings.S.RdsUpdStepLock,
                RdsExpansionStep.ExpandingVirtualDisk => Strings.S.RdsUpdStepVdisk,
                RdsExpansionStep.ResizingNtfsPartition => Strings.S.RdsUpdStepNtfs,
                RdsExpansionStep.VerifyingAndCompleting => Strings.S.RdsUpdStepVerify,
                _ => string.Empty
            };
        });

        try
        {
            string sid = SelectedUser.Sid;
            ulong addGb = (ulong)Math.Max(1, Math.Round(AdditionalGiB));

            var result = await _rdsService.ExpandUpdDiskAsync(
                broker: ConnectionBroker,
                collectionName: SelectedCollection.CollectionName,
                vhdxPath: DiskLayout.VhdxPath,
                userSid: sid,
                samAccountName: SelectedUser.SamAccountName,
                additionalGigabytes: addGb,
                progress: progress);

            ExpansionResult = result;

            if (result.IsSuccess)
            {
                WeakReferenceMessenger.Default.Send(
                    new AppNotificationMessage(string.Format(Strings.S.RdsUpdExpandSuccess, result.FinalCapacityGiB), InfoBarSeverity.Success));
                await InspectSelectedUserProfileDiskAsync();
            }
            else
            {
                WeakReferenceMessenger.Default.Send(
                    new AppNotificationMessage(result.ErrorMessage ?? "Disk expansion failed.", InfoBarSeverity.Error));
            }
        }
        catch (Exception ex)
        {
            ExpansionResult = new RdsDiskExpansionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage($"Disk expansion error: {ex.Message}", InfoBarSeverity.Error));
        }
        finally
        {
            IsExpanding = false;
            OnPropertyChanged(nameof(CanExpand));
        }
    }

    private void NotifyUpdProperties()
    {
        OnPropertyChanged(nameof(HasSelectedUser));
        OnPropertyChanged(nameof(HasDisk));
        OnPropertyChanged(nameof(IsDiskMissing));
        OnPropertyChanged(nameof(HasConflictingSessions));
        OnPropertyChanged(nameof(TargetGiB));
        OnPropertyChanged(nameof(CanExpand));
    }

    public void Dispose()
    {
        _filterDebounceTimer.Stop();
        _filterDebounceTimer.Dispose();
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
