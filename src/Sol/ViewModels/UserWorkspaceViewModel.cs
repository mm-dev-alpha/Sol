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

namespace Sol.ViewModels;

public partial class UserWorkspaceViewModel : ObservableObject
{
    private readonly IActiveDirectoryService _adService;
    private readonly IGreetingService _greetingService;
    private readonly ISettingsService _settings;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;

    public GlobalSearchViewModel Search { get; }

    [ObservableProperty]
    public partial bool IsJiraEnabled { get; set; }

    public UserWorkspaceViewModel(
        IActiveDirectoryService adService, 
        GlobalSearchViewModel search, 
        IGreetingService greetingService,
        ISettingsService settings,
        INavigationService navigationService,
        IExportService? exportService = null)
    {
        _adService = adService;
        Search = search;
        _greetingService = greetingService;
        _settings = settings;
        _navigationService = navigationService;
        _exportService = exportService ?? new ExportService();

        IsJiraEnabled = _settings.IsJiraEnabled;

        WeakReferenceMessenger.Default.Register<UserWorkspaceViewModel, JiraSettingsChangedMessage>(this, static (r, m) =>
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                r.IsJiraEnabled = m.IsEnabled;
            });
        });

        StartupGreeting = _greetingService.GetStartupGreeting();

        WeakReferenceMessenger.Default.Register<UserWorkspaceViewModel, UserSearchSelectedMessage>(this, static (r, m) =>
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await r.LoadUserAsync(m.Value);
                }
                catch (Exception ex)
                {
                    r.ShowError(ex.Message);
                }
            });
        });
    }

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial AdUser? CurrentUser { get; set; }
    
    // Multiple matches
    public ObservableCollection<AdUser> SearchResults { get; } = new();

    // State Flags
    public bool HasUser => CurrentUser != null;
    public bool IsEmptyState => CurrentUser == null && SearchResults.Count == 0;
    public bool HasMultipleMatches => SearchResults.Count > 0 && CurrentUser == null;

    // Derived properties for UI
    public bool HasManager => !string.IsNullOrWhiteSpace(CurrentUser?.Manager);
    public bool IsManagerDisplayVisible => !IsEditing && HasManager;
    public bool IsNoManagerDisplayVisible => !IsEditing && !HasManager;
    public bool HasDirectReports => CurrentUser?.DirectReports?.Count > 0;
    public string DirectReportsCount => CurrentUser?.DirectReports?.Count.ToString() ?? "0";
    public string DirectReportsCountBadge => CurrentUser?.DirectReports?.Count.ToString() ?? "0";
    public string MustChangePassword => CurrentUser?.PasswordLastSet == null || CurrentUser.PasswordLastSet == DateTime.MinValue || CurrentUser.PasswordLastSet.Value.Year < 1900 ? Strings.S.Yes : Strings.S.No;
    public bool IsAccountEnabled => CurrentUser?.AccountStatus == "Enabled";
    public bool IsAccountDisabled => CurrentUser?.AccountStatus == "Disabled";
    public string FormattedPasswordLastSet => CurrentUser?.PasswordLastSet?.ToString("d") ?? "N/A";
    public string FormattedLastLogon => CurrentUser?.LastLogon?.ToString("d") ?? "N/A";
    public string FormattedCreated => CurrentUser?.Created?.ToString("g") ?? "N/A";
    public string FormattedModified => CurrentUser?.Modified?.ToString("g") ?? "N/A";
    public string FormattedBadPasswordCount => CurrentUser != null ? CurrentUser.BadPasswordCount.ToString() : "0";

    [ObservableProperty]
    public partial string StartupGreeting { get; set; } = string.Empty;

    public ObservableCollection<string> GroupSearchSuggestions { get; } = new();
    public ObservableCollection<AdUser> ManagerSearchSuggestions { get; } = new();
    public ObservableCollection<AdAttributeItem> AdvancedAttributes { get; } = new();
    public ObservableCollection<string> FilteredGroups { get; } = new();
    public ObservableCollection<AdUser> CenterSuggestions { get; } = new();
    public ObservableCollection<AdComputer> CenterComputerSuggestions { get; } = new();

    [ObservableProperty] public partial string GroupFilterQuery { get; set; } = string.Empty;
    [ObservableProperty] public partial string CenterSearchQuery { get; set; } = string.Empty;
    [ObservableProperty] public partial string CenterComputerSearchQuery { get; set; } = string.Empty;

    private List<AdAttributeItem> _allAdvancedAttributes = new();

    [RelayCommand]
    public void ResetToHeroState()
    {
        CurrentUser = null;
        SearchResults.Clear();
        IsEditing = false;
        IsAdvancedEditorOpen = false;
        CenterSearchQuery = string.Empty;
        CenterComputerSearchQuery = string.Empty;
        CenterSuggestions.Clear();
        CenterComputerSuggestions.Clear();
        NotifyPropertiesChanged();
    }

    public string GroupCountBadge => CurrentUser?.Groups?.Count.ToString() ?? "0";
    public bool HasNoFilteredGroups => FilteredGroups.Count == 0;

    [RelayCommand]
    public void FilterGroups(string query)
    {
        GroupFilterQuery = query;
        RefreshFilteredGroups();
    }

    public void RefreshFilteredGroups()
    {
        var groups = CurrentUser?.Groups ?? new List<string>();
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
            var results = await _adService.SearchUsersAsync(query);
            CenterSuggestions.Clear();
            foreach (var u in results) CenterSuggestions.Add(u);
        }
        catch
        {
            // Silent ignore suggestion failure
        }
    }

    [RelayCommand]
    public async Task SearchCenterComputersAsync(string query)
    {
        CenterComputerSearchQuery = query;
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            CenterComputerSuggestions.Clear();
            return;
        }

        try
        {
            var results = await _adService.SearchComputersAsync(query);
            CenterComputerSuggestions.Clear();
            foreach (var c in results) CenterComputerSuggestions.Add(c);
        }
        catch
        {
            // Silent ignore suggestion failure
        }
    }

    [RelayCommand]
    private void FilterAttributes(string query)
    {
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allAdvancedAttributes
            : _allAdvancedAttributes.Where(item => item.Key.Contains(query, StringComparison.OrdinalIgnoreCase) || item.Value.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        AdvancedAttributes.Clear();
        foreach (var item in filtered)
        {
            AdvancedAttributes.Add(item);
        }
    }

    [ObservableProperty] public partial bool IsEditing { get; set; }
    [ObservableProperty] public partial bool IsAdvancedEditorOpen { get; set; }

    partial void OnIsEditingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsManagerDisplayVisible));
        OnPropertyChanged(nameof(IsNoManagerDisplayVisible));
    }
    [ObservableProperty] public partial string EditTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditDepartment { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditManager { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditOffice { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditOfficePhone { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditMobilePhone { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditAddress { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditCity { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditState { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditPostalCode { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditGivenName { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditSurname { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditEmail { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditWebPage { get; set; } = string.Empty;
    [ObservableProperty] public partial string EditManagerSamAccountName { get; set; } = string.Empty;
    [ObservableProperty] public partial string NewGroupName { get; set; } = string.Empty;
    [ObservableProperty] public partial string AttributeEditorErrorMessage { get; set; } = string.Empty;

    public void NotifyPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasUser));
        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(HasMultipleMatches));
        OnPropertyChanged(nameof(HasManager));
        OnPropertyChanged(nameof(IsManagerDisplayVisible));
        OnPropertyChanged(nameof(IsNoManagerDisplayVisible));
        OnPropertyChanged(nameof(HasDirectReports));
        OnPropertyChanged(nameof(DirectReportsCount));
        OnPropertyChanged(nameof(DirectReportsCountBadge));
        OnPropertyChanged(nameof(MustChangePassword));
        OnPropertyChanged(nameof(IsAccountEnabled));
        OnPropertyChanged(nameof(IsAccountDisabled));
        OnPropertyChanged(nameof(FormattedPasswordLastSet));
        OnPropertyChanged(nameof(FormattedLastLogon));
        OnPropertyChanged(nameof(FormattedCreated));
        OnPropertyChanged(nameof(FormattedModified));
        OnPropertyChanged(nameof(FormattedBadPasswordCount));
        OnPropertyChanged(nameof(GroupCountBadge));
        OnPropertyChanged(nameof(HasNoFilteredGroups));
    }

    public async Task LoadUserAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        IsLoading = true;
        CurrentUser = null;
        SearchResults.Clear();
        NotifyPropertiesChanged();

        try
        {
            var results = await _adService.SearchUsersAsync(query);
            
            if (results.Count == 0)
            {
                ShowError(Strings.NoUsersFound(query));
            }
            else if (results.Count == 1)
            {
                CurrentUser = results[0];
                NotifyPropertiesChanged();
                RefreshFilteredGroups();
                IsEditing = false;
                SyncEditFields();
                AdvancedAttributes.Clear();
            }
            else
            {
                var exactUser = results.FirstOrDefault(u => u.SamAccountName.Equals(query, StringComparison.OrdinalIgnoreCase));
                if (exactUser != null)
                {
                    CurrentUser = exactUser;
                    NotifyPropertiesChanged();
                    RefreshFilteredGroups();
                    IsEditing = false;
                    SyncEditFields();
                    AdvancedAttributes.Clear();
                }
                else
                {
                    foreach (var r in results) SearchResults.Add(r);
                    NotifyPropertiesChanged();
                }
            }
        }
        catch (Exception ex)
        {
            ShowError(Strings.ErrorLoadingUser(ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToUserAsync(string samAccountName)
    {
        if (string.IsNullOrWhiteSpace(samAccountName)) return;
        await LoadUserAsync(samAccountName);
    }

    // --- QUICK ACTIONS ---

    [RelayCommand]
    private async Task UnlockAccountAsync()
    {
        if (CurrentUser == null) return;
        try
        {
            await _adService.UnlockAccountAsync(CurrentUser.SamAccountName);
            ShowInfo(Strings.S.AccountUnlockedSuccess);
            await RefreshCurrentUserAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    [RelayCommand]
    private async Task EnableAccountAsync()
    {
        if (CurrentUser == null) return;
        try
        {
            await _adService.EnableAccountAsync(CurrentUser.SamAccountName, true);
            ShowInfo(Strings.S.AccountEnabledSuccess);
            await RefreshCurrentUserAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    [RelayCommand]
    private async Task DisableAccountAsync()
    {
        if (CurrentUser == null) return;
        try
        {
            await _adService.EnableAccountAsync(CurrentUser.SamAccountName, false);
            ShowInfo(Strings.S.AccountDisabledSuccess);
            await RefreshCurrentUserAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    [RelayCommand]
    public async Task ResetPasswordWithPolicyAsync(Tuple<string, bool, bool> args)
    {
        var newPassword = args.Item1;
        var requireChange = args.Item2;
        var unlockAccount = args.Item3;

        if (CurrentUser == null || string.IsNullOrWhiteSpace(newPassword)) return;
        IsLoading = true;
        try
        {
            await _adService.ResetPasswordAsync(CurrentUser.SamAccountName, newPassword, requireChange);
            if (unlockAccount && CurrentUser.IsLockedOut)
            {
                await _adService.UnlockAccountAsync(CurrentUser.SamAccountName);
            }
            
            if (SafeClipboard.TrySetText(newPassword, isSensitive: true))
            {
                ShowInfo(Strings.S.PasswordResetSuccess);
            }
            else
            {
                ShowError(Strings.S.ClipboardBusy);
            }
            await RefreshCurrentUserAsync();
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

    public static string GenerateSecurePassword()
    {
        const string uppers = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowers = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string specials = "!@#$%^&*-_+=";
        const string allChars = uppers + lowers + digits + specials;

        var chars = new char[16];
        try
        {
            chars[0] = uppers[System.Security.Cryptography.RandomNumberGenerator.GetInt32(uppers.Length)];
            chars[1] = lowers[System.Security.Cryptography.RandomNumberGenerator.GetInt32(lowers.Length)];
            chars[2] = digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)];
            chars[3] = specials[System.Security.Cryptography.RandomNumberGenerator.GetInt32(specials.Length)];

            for (int i = 4; i < 16; i++)
            {
                chars[i] = allChars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(allChars.Length)];
            }

            // Cryptographically secure in-place Fisher-Yates shuffle
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }
        finally
        {
            Array.Clear(chars, 0, chars.Length);
        }
    }

    [RelayCommand]
    private async Task ForcePasswordChangeAsync()
    {
        if (CurrentUser == null) return;
        try
        {
            await _adService.ForcePasswordChangeAsync(CurrentUser.SamAccountName);
            ShowInfo(Strings.S.ForcePasswordChangeSuccess);
            await RefreshCurrentUserAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    [RelayCommand]
    private void NavigateToJira()
    {
        if (CurrentUser != null)
        {
            _navigationService.NavigateTo("JiraWorkspacePage", CurrentUser);
        }
    }

    // --- PROFILE EDITING ---

    [RelayCommand]
    private void BeginEdit()
    {
        SyncEditFields();
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        SyncEditFields();
    }

    private void SyncEditFields()
    {
        if (CurrentUser == null) return;
        EditTitle = CurrentUser.Title;
        EditDepartment = CurrentUser.Department;
        EditManager = CurrentUser.Manager;
        EditOffice = CurrentUser.Office;
        EditOfficePhone = CurrentUser.OfficePhone;
        EditMobilePhone = CurrentUser.MobilePhone;
        EditAddress = CurrentUser.StreetAddress;
        EditCity = CurrentUser.City;
        EditState = CurrentUser.State;
        EditPostalCode = CurrentUser.PostalCode;
        EditGivenName = CurrentUser.GivenName;
        EditSurname = CurrentUser.Surname;
        EditEmail = CurrentUser.Email;
        EditWebPage = CurrentUser.WebPage;
        EditManagerSamAccountName = string.Empty;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (CurrentUser == null) return;
        IsLoading = true;
        try
        {
            var updates = new Dictionary<string, string>();
            
            if (EditTitle != CurrentUser.Title) updates["title"] = EditTitle;
            if (EditDepartment != CurrentUser.Department) updates["department"] = EditDepartment;
            if (EditOffice != CurrentUser.Office) updates["physicalDeliveryOfficeName"] = EditOffice;
            if (EditOfficePhone != CurrentUser.OfficePhone) updates["telephoneNumber"] = EditOfficePhone;
            if (EditMobilePhone != CurrentUser.MobilePhone) updates["mobile"] = EditMobilePhone;
            if (EditAddress != CurrentUser.StreetAddress) updates["streetAddress"] = EditAddress;
            if (EditCity != CurrentUser.City) updates["l"] = EditCity;
            if (EditState != CurrentUser.State) updates["st"] = EditState;
            if (EditPostalCode != CurrentUser.PostalCode) updates["postalCode"] = EditPostalCode;
            if (EditGivenName != CurrentUser.GivenName) updates["givenName"] = EditGivenName;
            if (EditSurname != CurrentUser.Surname) updates["sn"] = EditSurname;
            if (EditEmail != CurrentUser.Email) updates["mail"] = EditEmail;
            if (EditWebPage != CurrentUser.WebPage) updates["wWWHomePage"] = EditWebPage;

            string? newManager = null;
            if (!string.IsNullOrWhiteSpace(EditManagerSamAccountName))
            {
                newManager = EditManagerSamAccountName;
            }
            else if (string.IsNullOrWhiteSpace(EditManager) && !string.IsNullOrWhiteSpace(CurrentUser.Manager))
            {
                newManager = "";
            }

            if (updates.Count > 0 || newManager != null)
            {
                await _adService.UpdateUserProfileAsync(CurrentUser.SamAccountName, updates, newManager);
                ShowInfo(Strings.S.ProfileUpdatedSuccess);
                await LoadUserAsync(CurrentUser.SamAccountName);
            }
            else
            {
                IsEditing = false;
            }
        }
        catch (Exception ex)
        {
            ShowError(Strings.SaveProfileFailed(ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    // --- MANAGER AUTO-SUGGEST ---

    [RelayCommand]
    private async Task SearchManagerAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            ManagerSearchSuggestions.Clear();
            return;
        }

        try
        {
            var results = await _adService.SearchUsersAsync(query);
            ManagerSearchSuggestions.Clear();
            foreach (var user in results)
            {
                ManagerSearchSuggestions.Add(user);
            }
        }
        catch
        {
            // Ignore suggestions failure
        }
    }

    [RelayCommand]
    private void ManagerSelected(object? selectedItem)
    {
        if (selectedItem is AdUser user)
        {
            EditManager = user.DisplayName;
            EditManagerSamAccountName = user.SamAccountName;
        }
    }

    // --- GROUPS ---

    [RelayCommand]
    private async Task SearchGroupsAsync(string query)
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
        catch { }
    }

    [RelayCommand]
    private async Task AddToGroupAsync()
    {
        var groupName = NewGroupName;
        if (CurrentUser == null || string.IsNullOrWhiteSpace(groupName)) return;
        IsLoading = true;
        try
        {
            await _adService.AddUserToGroupAsync(CurrentUser.SamAccountName, groupName);
            ShowInfo(Strings.AddedToGroupSuccess(groupName));
            NewGroupName = string.Empty;
            await RefreshCurrentUserAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RemoveFromGroupAsync(string groupName)
    {
        if (CurrentUser == null || string.IsNullOrWhiteSpace(groupName)) return;
        IsLoading = true;
        try
        {
            await _adService.RemoveUserFromGroupAsync(CurrentUser.SamAccountName, groupName);
            ShowInfo(Strings.RemovedFromGroupSuccess(groupName));
            await RefreshCurrentUserAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsLoading = false; }
    }

    // --- ADVANCED ATTRIBUTE INSPECTOR & SAFE EDITOR ---

    [ObservableProperty] public partial KeyValuePair<string, string>? SelectedAttribute { get; set; }
    [ObservableProperty] public partial string EditAttributeNewValue { get; set; } = string.Empty;

    public bool IsSelectedAttributeEditable => SelectedAttribute.HasValue && ActiveDirectoryService.IsAttributeEditable(SelectedAttribute.Value.Key);

    [RelayCommand]
    private async Task ToggleAdvancedEditorAsync()
    {
        IsAdvancedEditorOpen = !IsAdvancedEditorOpen;
        if (IsAdvancedEditorOpen && AdvancedAttributes.Count == 0)
        {
            await LoadAdvancedAttributesAsync();
        }
    }

    [RelayCommand]
    private async Task LoadAdvancedAttributesAsync()
    {
        if (CurrentUser == null) return;
        IsLoading = true;
        try
        {
            var rawAttrs = await _adService.GetAllUserAttributesAsync(CurrentUser.SamAccountName);
            var attrs = rawAttrs.Select(kvp => new AdAttributeItem(kvp.Key, kvp.Value)).ToList();
            _allAdvancedAttributes = attrs;

            AdvancedAttributes.Clear();
            foreach (var item in attrs)
            {
                AdvancedAttributes.Add(item);
            }
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task CommitAttributeEditAsync(Tuple<string, string> args)
    {
        if (CurrentUser == null || string.IsNullOrWhiteSpace(args.Item1)) return;
        IsLoading = true;
        AttributeEditorErrorMessage = string.Empty;
        try
        {
            await _adService.UpdateRawAttributeAsync(CurrentUser.SamAccountName, args.Item1, args.Item2);
            ShowInfo(Strings.AttributeUpdateSuccess(args.Item1));
            await LoadAdvancedAttributesAsync();
            await RefreshCurrentUserAsync();
        }
        catch (Exception ex)
        {
            AttributeEditorErrorMessage = ex.Message;
            ShowError(Strings.AttributeUpdateFailed(args.Item1, ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshCurrentUserAsync()
    {
        if (CurrentUser == null) return;
        try
        {
            var results = await _adService.SearchUsersAsync(CurrentUser.SamAccountName);
            var refreshed = results.FirstOrDefault(u => u.SamAccountName.Equals(CurrentUser.SamAccountName, StringComparison.OrdinalIgnoreCase)) ?? results.FirstOrDefault();
            if (refreshed != null)
            {
                CurrentUser = refreshed;
                NotifyPropertiesChanged();
                RefreshFilteredGroups();
                SyncEditFields();
            }
        }
        catch
        {
            // Silent fallback
        }
    }

    // --- CLIPBOARD & QUICK ACTIONS ---

    [RelayCommand]
    private void OpenCompareWith()
    {
        if (CurrentUser == null) return;
        WeakReferenceMessenger.Default.Send(new InitiateComparisonMessage(ComparisonMode.Users, CurrentUser));
        _navigationService.NavigateTo("CompareWorkspacePage");
    }

    [RelayCommand]
    private void CopyPowerShell()
    {
        if (CurrentUser == null) return;
        string safeSam = (CurrentUser.SamAccountName ?? string.Empty).Replace("'", "''");
        var ps = $"Get-ADUser -Identity '{safeSam}' -Properties *";
        if (SafeClipboard.TrySetText(ps))
        {
            ShowInfo(Strings.S.PowerShellCommandCopied);
        }
        else
        {
            ShowError(Strings.S.ClipboardBusy);
        }
    }

    [RelayCommand]
    private void CopyAll()
    {
        if (CurrentUser == null) return;
        var report = _exportService.FormatUserProfileReport(CurrentUser);
        if (SafeClipboard.TrySetText(report))
        {
            ShowInfo(Strings.S.AllInfoCopiedSuccess);
        }
        else
        {
            ShowError(Strings.S.ClipboardBusy);
        }
    }

    [RelayCommand]
    private void CopyToClipboard(object? parameter)
    {
        string? text = parameter?.ToString();
        if (string.IsNullOrEmpty(text)) return;
        if (SafeClipboard.TrySetText(text))
        {
            ShowInfo(Strings.S.CopiedToClipboard);
        }
        else
        {
            ShowError(Strings.S.ClipboardBusy);
        }
    }

    [RelayCommand]
    private void CloseWorkspace()
    {
        CurrentUser = null;
        SearchResults.Clear();
        IsEditing = false;
        IsAdvancedEditorOpen = false;
        NotifyPropertiesChanged();
    }

    public void ShowInfo(string message)
    {
        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(message, AppNotificationSeverity.Informational));
    }

    public void ShowError(string message)
    {
        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(message, AppNotificationSeverity.Error));
    }
}


