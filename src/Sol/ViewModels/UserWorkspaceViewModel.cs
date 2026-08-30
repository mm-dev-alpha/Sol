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

public partial class UserWorkspaceViewModel : ObservableObject
{
    private readonly IActiveDirectoryService _adService;
    private readonly IGreetingService _greetingService;
    private readonly ISettingsService _settings;
    private readonly INavigationService _navigationService;

    public GlobalSearchViewModel Search { get; }

    [ObservableProperty]
    public partial bool IsJiraEnabled { get; set; }

    public UserWorkspaceViewModel(
        IActiveDirectoryService adService, 
        GlobalSearchViewModel search, 
        IGreetingService greetingService,
        ISettingsService settings,
        INavigationService navigationService)
    {
        _adService = adService;
        Search = search;
        _greetingService = greetingService;
        _settings = settings;
        _navigationService = navigationService;

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

    // Visibility States
    public Visibility UserContentVisibility => CurrentUser != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyStateVisibility => CurrentUser == null && SearchResults.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MultipleMatchesVisibility => SearchResults.Count > 0 && CurrentUser == null ? Visibility.Visible : Visibility.Collapsed;

    // Derived properties for UI
    public bool HasManager => !string.IsNullOrWhiteSpace(CurrentUser?.Manager);
    public Visibility ManagerDisplayVisibility => !IsEditing && HasManager ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NoManagerDisplayVisibility => !IsEditing && !HasManager ? Visibility.Visible : Visibility.Collapsed;
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
        OnPropertyChanged(nameof(ManagerDisplayVisibility));
        OnPropertyChanged(nameof(NoManagerDisplayVisibility));
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
        OnPropertyChanged(nameof(UserContentVisibility));
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(MultipleMatchesVisibility));
        OnPropertyChanged(nameof(HasManager));
        OnPropertyChanged(nameof(ManagerDisplayVisibility));
        OnPropertyChanged(nameof(NoManagerDisplayVisibility));
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
            
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(newPassword);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            
            ShowInfo(Strings.S.PasswordResetSuccess);
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
    private void ManagerSelected(AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AdUser user)
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
    private void CopyPowerShell()
    {
        if (CurrentUser == null) return;
        var ps = $"Get-ADUser -Identity \"{CurrentUser.SamAccountName}\" -Properties *";
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(ps);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        ShowInfo(Strings.S.PowerShellCommandCopied);
    }

    [RelayCommand]
    private void CopyAll()
    {
        if (CurrentUser == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine($"ACTIVE DIRECTORY USER PROFILE: {CurrentUser.DisplayName} ({CurrentUser.SamAccountName})");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        // 1. Identity & Directory
        sb.AppendLine("[ IDENTITY & DIRECTORY ]");
        sb.AppendLine($"  Display Name:        {CurrentUser.DisplayName}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.GivenName))
            sb.AppendLine($"  First Name:          {CurrentUser.GivenName}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.Surname))
            sb.AppendLine($"  Last Name:           {CurrentUser.Surname}");
        sb.AppendLine($"  SAM Account Name:    {CurrentUser.SamAccountName}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.Upn))
            sb.AppendLine($"  User Principal Name: {CurrentUser.Upn}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.Email))
            sb.AppendLine($"  Email Address:       {CurrentUser.Email}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.EmployeeId))
            sb.AppendLine($"  Employee ID:         {CurrentUser.EmployeeId}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.Sid))
            sb.AppendLine($"  Security ID (SID):   {CurrentUser.Sid}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.OuPath))
            sb.AppendLine($"  OU Path:             {CurrentUser.OuPath}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.Description))
            sb.AppendLine($"  Description:         {CurrentUser.Description}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.WebPage))
            sb.AppendLine($"  Web Page:            {CurrentUser.WebPage}");
        sb.AppendLine();

        // 2. Organization
        sb.AppendLine("[ ORGANIZATION ]");
        sb.AppendLine($"  Job Title:           {(!string.IsNullOrWhiteSpace(CurrentUser.Title) ? CurrentUser.Title : "—")}");
        sb.AppendLine($"  Department:          {(!string.IsNullOrWhiteSpace(CurrentUser.Department) ? CurrentUser.Department : "—")}");
        sb.AppendLine($"  Office:              {(!string.IsNullOrWhiteSpace(CurrentUser.Office) ? CurrentUser.Office : "—")}");
        sb.AppendLine($"  Manager:             {(!string.IsNullOrWhiteSpace(CurrentUser.Manager) ? CurrentUser.Manager : "—")}");
        if (CurrentUser.DirectReports != null && CurrentUser.DirectReports.Count > 0)
        {
            sb.AppendLine($"  Direct Reports ({CurrentUser.DirectReports.Count}):");
            foreach (var report in CurrentUser.DirectReports)
            {
                sb.AppendLine($"    - {report}");
            }
        }
        else
        {
            sb.AppendLine("  Direct Reports:      None");
        }
        sb.AppendLine();

        // 3. Contact Details
        sb.AppendLine("[ CONTACT INFORMATION ]");
        sb.AppendLine($"  Office Phone:        {(!string.IsNullOrWhiteSpace(CurrentUser.OfficePhone) ? CurrentUser.OfficePhone : "—")}");
        sb.AppendLine($"  Mobile Phone:        {(!string.IsNullOrWhiteSpace(CurrentUser.MobilePhone) ? CurrentUser.MobilePhone : "—")}");
        if (!string.IsNullOrWhiteSpace(CurrentUser.StreetAddress) || !string.IsNullOrWhiteSpace(CurrentUser.City) || !string.IsNullOrWhiteSpace(CurrentUser.PostalCode) || !string.IsNullOrWhiteSpace(CurrentUser.State))
        {
            sb.AppendLine($"  Street Address:      {(!string.IsNullOrWhiteSpace(CurrentUser.StreetAddress) ? CurrentUser.StreetAddress : "—")}");
            sb.AppendLine($"  City:                {(!string.IsNullOrWhiteSpace(CurrentUser.City) ? CurrentUser.City : "—")}");
            sb.AppendLine($"  Postal Code:         {(!string.IsNullOrWhiteSpace(CurrentUser.PostalCode) ? CurrentUser.PostalCode : "—")}");
            sb.AppendLine($"  State / Province:    {(!string.IsNullOrWhiteSpace(CurrentUser.State) ? CurrentUser.State : "—")}");
        }
        sb.AppendLine();

        // 4. Account & Security Status
        sb.AppendLine("[ ACCOUNT & SECURITY STATUS ]");
        sb.AppendLine($"  Account Status:      {CurrentUser.AccountStatus}");
        sb.AppendLine($"  Locked Out:          {(CurrentUser.IsLockedOut ? Strings.S.Yes : Strings.S.No)}");
        sb.AppendLine($"  Account Expires:     {(CurrentUser.AccountExpires.HasValue ? CurrentUser.AccountExpires.Value.ToString("g") : (!string.IsNullOrWhiteSpace(CurrentUser.AccountExpiresStatus) ? CurrentUser.AccountExpiresStatus : Strings.S.Never))}");
        sb.AppendLine($"  Password Last Set:   {FormattedPasswordLastSet}");
        sb.AppendLine($"  Password Expiry:     {(!string.IsNullOrWhiteSpace(CurrentUser.PasswordExpiryStatus) ? CurrentUser.PasswordExpiryStatus : (CurrentUser.PasswordExpiry.HasValue ? CurrentUser.PasswordExpiry.Value.ToString("g") : Strings.S.Never))}");
        sb.AppendLine($"  Password Never Exp.: {(CurrentUser.PasswordNeverExpires ? Strings.S.Yes : Strings.S.No)}");
        sb.AppendLine($"  Bad Password Count:  {CurrentUser.BadPasswordCount}");
        if (CurrentUser.BadPasswordTime.HasValue && CurrentUser.BadPasswordTime.Value != DateTime.MinValue)
            sb.AppendLine($"  Last Bad Password:   {CurrentUser.BadPasswordTime.Value:g}");
        sb.AppendLine();

        // 5. Activity & Object Metadata
        sb.AppendLine("[ ACTIVITY & OBJECT METADATA ]");
        sb.AppendLine($"  Last Logon:          {FormattedLastLogon}");
        if (CurrentUser.LastLogonTimestamp.HasValue)
            sb.AppendLine($"  Last Logon Timestamp:{CurrentUser.LastLogonTimestamp.Value:g}");
        sb.AppendLine($"  Created:             {FormattedCreated}");
        sb.AppendLine($"  Modified:            {FormattedModified}");
        sb.AppendLine();

        // 6. Security Groups
        sb.AppendLine($"[ GROUP MEMBERSHIPS ({CurrentUser.Groups.Count}) ]");
        if (CurrentUser.Groups.Count > 0)
        {
            foreach (var group in CurrentUser.Groups)
            {
                sb.AppendLine($"  - {group}");
            }
        }
        else
        {
            sb.AppendLine("  (No groups assigned)");
        }
        
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(sb.ToString());
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        ShowInfo(Strings.S.AllInfoCopiedSuccess);
    }

    [RelayCommand]
    private void CopyToClipboard(object? parameter)
    {
        string? text = parameter?.ToString();
        if (string.IsNullOrEmpty(text)) return;
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(text);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        ShowInfo(Strings.S.CopiedToClipboard);
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
        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(message, InfoBarSeverity.Informational));
    }

    public void ShowError(string message)
    {
        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(message, InfoBarSeverity.Error));
    }
}


