using System;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Sol.Services;
using Sol.Helpers;
using Sol.Models;

namespace Sol.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IJiraService _jiraService;
    private readonly IFileLocksmithService? _fileLocksmithService;

    [ObservableProperty] public partial string Version { get; set; } = typeof(SettingsViewModel).Assembly.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "3.6.1";
    [ObservableProperty] public partial string AdDomain { get; set; } = string.Empty;
    public string AppLanguage => "en";

    public string[] JiraDeploymentModes => [Strings.S.JiraDataCenterOption, Strings.S.JiraCloudOption];

    // JIRA Integration
    [ObservableProperty] public partial bool IsJiraEnabled { get; set; }
    [ObservableProperty] public partial string JiraDeploymentMode { get; set; } = "DataCenter";
    [ObservableProperty] public partial int JiraDeploymentModeIndex { get; set; }
    [ObservableProperty] public partial string JiraBaseUrl { get; set; } = string.Empty;
    [ObservableProperty] public partial string JiraCloudEmail { get; set; } = string.Empty;

    // Isolated per-mode secrets
    [ObservableProperty] public partial string JiraPatSecret { get; set; } = string.Empty;
    [ObservableProperty] public partial string JiraCloudTokenSecret { get; set; } = string.Empty;

    // Tools Configuration
    [ObservableProperty] public partial bool IsShortcutGuideEnabled { get; set; } = true;
    [ObservableProperty] public partial bool IsFileLocksmithShellIntegrationEnabled { get; set; }
    [ObservableProperty] public partial bool IsMmcLookupEnabled { get; set; } = true;
    [ObservableProperty] public partial bool IsAdminCommandsEnabled { get; set; } = true;
    [ObservableProperty] public partial bool IsGrabFrameEnabled { get; set; }
    [ObservableProperty] public partial bool GrabFrameAutoOcr { get; set; }
    [ObservableProperty] public partial bool GrabFrameAlwaysOnTop { get; set; }
    [ObservableProperty] public partial bool GrabFrameSingleLine { get; set; }
    [ObservableProperty] public partial bool GrabFrameTableMode { get; set; }
    [ObservableProperty] public partial bool GrabFrameAutoPaste { get; set; }
    [ObservableProperty] public partial bool IsEditTextWindowEnabled { get; set; }
    [ObservableProperty] public partial bool EditTextWindowWordWrap { get; set; }
    [ObservableProperty] public partial bool EditTextWindowAlwaysOnTop { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotTestingJira))]
    public partial bool IsTestingJira { get; set; }
    [ObservableProperty] public partial bool IsJiraTestStatusOpen { get; set; }
    [ObservableProperty] public partial InfoBarSeverity JiraTestStatusSeverity { get; set; } = InfoBarSeverity.Informational;
    [ObservableProperty] public partial string JiraTestStatusMessage { get; set; } = string.Empty;

    public bool IsNotTestingJira => !IsTestingJira;
    public bool IsJiraDataCenter => string.Equals(JiraDeploymentMode, "DataCenter", StringComparison.OrdinalIgnoreCase);
    public bool IsJiraCloud => string.Equals(JiraDeploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase);

    public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "3.6.1.0";

    public SettingsViewModel(ISettingsService settings, IJiraService jiraService, IFileLocksmithService? fileLocksmithService = null)
    {
        _settings = settings;
        _jiraService = jiraService;
        _fileLocksmithService = fileLocksmithService;
        LoadSettings();
    }

    public void LoadSettings()
    {
        AdDomain = _settings.AdDomain;

        IsJiraEnabled = _settings.IsJiraEnabled;
        JiraDeploymentMode = _settings.JiraDeploymentMode ?? "DataCenter";
        JiraDeploymentModeIndex = string.Equals(JiraDeploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        JiraBaseUrl = _settings.JiraBaseUrl ?? string.Empty;
        JiraCloudEmail = _settings.JiraCloudEmail ?? string.Empty;

        // Load isolated secrets from Windows Credential Locker
        JiraPatSecret = JiraCredentialHelper.GetSecret("DataCenter");
        JiraCloudTokenSecret = JiraCredentialHelper.GetSecret("Cloud");

        // Tools settings
        IsShortcutGuideEnabled = _settings.IsShortcutGuideEnabled;
        IsFileLocksmithShellIntegrationEnabled = _fileLocksmithService?.IsContextMenuRegistered() ?? _settings.IsFileLocksmithShellIntegrationEnabled;
        IsMmcLookupEnabled = _settings.IsMmcLookupEnabled;
        IsAdminCommandsEnabled = _settings.IsAdminCommandsEnabled;
        IsGrabFrameEnabled = _settings.IsGrabFrameEnabled;
        GrabFrameAutoOcr = _settings.GrabFrameAutoOcr;
        GrabFrameAlwaysOnTop = _settings.GrabFrameAlwaysOnTop;
        GrabFrameSingleLine = _settings.GrabFrameSingleLine;
        GrabFrameTableMode = _settings.GrabFrameTableMode;
        GrabFrameAutoPaste = _settings.GrabFrameAutoPaste;
        IsEditTextWindowEnabled = _settings.IsEditTextWindowEnabled;
        EditTextWindowWordWrap = _settings.EditTextWindowWordWrap;
        EditTextWindowAlwaysOnTop = _settings.EditTextWindowAlwaysOnTop;

        OnPropertyChanged(nameof(IsJiraDataCenter));
        OnPropertyChanged(nameof(IsJiraCloud));
    }

    partial void OnIsJiraEnabledChanged(bool value)
    {
        // Real-time synchronization without requiring application restart
        _settings.IsJiraEnabled = value;
        _settings.Save();
        WeakReferenceMessenger.Default.Send(new JiraSettingsChangedMessage(value));
    }

    partial void OnJiraDeploymentModeIndexChanged(int value)
    {
        if (value < 0) return; // Ignore transient unselected index during ComboBox layout
        JiraDeploymentMode = value == 1 ? "Cloud" : "DataCenter";
    }

    partial void OnJiraDeploymentModeChanged(string value)
    {
        int newIndex = string.Equals(value, "Cloud", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        if (JiraDeploymentModeIndex != newIndex)
        {
            JiraDeploymentModeIndex = newIndex;
        }
        OnPropertyChanged(nameof(IsJiraDataCenter));
        OnPropertyChanged(nameof(IsJiraCloud));
    }

    [RelayCommand]
    private async Task TestJiraConnectionAsync()
    {
        if (IsTestingJira) return;

        string baseUrl = (JiraBaseUrl ?? string.Empty).Trim();
        string secretToUse = (IsJiraCloud ? JiraCloudTokenSecret : JiraPatSecret) ?? string.Empty;
        string email = (JiraCloudEmail ?? string.Empty).Trim();

        // 1. Validation guard: Base URL format (HTTPS strictly enforced)
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedUri) ||
            parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            JiraTestStatusMessage = Strings.S.JiraHttpsRequiredPrompt;
            JiraTestStatusSeverity = InfoBarSeverity.Warning;
            IsJiraTestStatusOpen = true;
            WeakReferenceMessenger.Default.Send(new AppNotificationMessage(Strings.S.JiraHttpsRequiredPrompt, InfoBarSeverity.Warning));
            return;
        }

        // 2. Validation guard: Atlassian Cloud email requirement
        if (IsJiraCloud && string.IsNullOrWhiteSpace(email))
        {
            JiraTestStatusMessage = Strings.S.JiraEmailRequiredPrompt;
            JiraTestStatusSeverity = InfoBarSeverity.Warning;
            IsJiraTestStatusOpen = true;
            WeakReferenceMessenger.Default.Send(new AppNotificationMessage(Strings.S.JiraEmailRequiredPrompt, InfoBarSeverity.Warning));
            return;
        }

        // 3. Validation guard: Access Token / PAT requirement
        if (string.IsNullOrWhiteSpace(secretToUse.Trim()))
        {
            JiraTestStatusMessage = Strings.S.JiraSecretRequiredPrompt;
            JiraTestStatusSeverity = InfoBarSeverity.Warning;
            IsJiraTestStatusOpen = true;
            WeakReferenceMessenger.Default.Send(new AppNotificationMessage(Strings.S.JiraSecretRequiredPrompt, InfoBarSeverity.Warning));
            return;
        }

        IsTestingJira = true;
        IsJiraTestStatusOpen = false;
        try
        {
            bool success = await _jiraService.TestConnectionAsync(
                overrideBaseUrl: baseUrl,
                overrideMode: JiraDeploymentMode,
                overrideEmail: email,
                overrideSecret: secretToUse.Trim());

            if (success)
            {
                JiraTestStatusMessage = Strings.S.JiraConnectionSuccessPrompt;
                JiraTestStatusSeverity = InfoBarSeverity.Success;
                IsJiraTestStatusOpen = true;
                WeakReferenceMessenger.Default.Send(new AppNotificationMessage(Strings.S.JiraConnectionSuccessPrompt, InfoBarSeverity.Success));
            }
            else
            {
                JiraTestStatusMessage = Strings.S.JiraConnectionFailedPrompt;
                JiraTestStatusSeverity = InfoBarSeverity.Error;
                IsJiraTestStatusOpen = true;
                WeakReferenceMessenger.Default.Send(new AppNotificationMessage(Strings.S.JiraConnectionFailedPrompt, InfoBarSeverity.Error));
            }
        }
        catch (Exception ex)
        {
            JiraTestStatusMessage = $"{Strings.S.JiraConnectionFailedPrompt}: {ex.Message}";
            JiraTestStatusSeverity = InfoBarSeverity.Error;
            IsJiraTestStatusOpen = true;
            WeakReferenceMessenger.Default.Send(new AppNotificationMessage($"{Strings.S.JiraConnectionFailedPrompt}: {ex.Message}", InfoBarSeverity.Error));
        }
        finally
        {
            IsTestingJira = false;
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            _settings.AdDomain = AdDomain;
            _settings.AppLanguage = AppLanguage;

            _settings.IsJiraEnabled = IsJiraEnabled;
            _settings.JiraDeploymentMode = JiraDeploymentMode;
            _settings.JiraBaseUrl = JiraBaseUrl;
            _settings.JiraCloudEmail = JiraCloudEmail;

            // Save Tools Configuration
            _settings.IsShortcutGuideEnabled = IsShortcutGuideEnabled;
            _settings.IsFileLocksmithShellIntegrationEnabled = IsFileLocksmithShellIntegrationEnabled;
            _settings.IsMmcLookupEnabled = IsMmcLookupEnabled;
            _settings.IsAdminCommandsEnabled = IsAdminCommandsEnabled;
            _settings.IsGrabFrameEnabled = IsGrabFrameEnabled;
            _settings.GrabFrameAutoOcr = GrabFrameAutoOcr;
            _settings.GrabFrameAlwaysOnTop = GrabFrameAlwaysOnTop;
            _settings.GrabFrameSingleLine = GrabFrameSingleLine;
            _settings.GrabFrameTableMode = GrabFrameTableMode;
            _settings.GrabFrameAutoPaste = GrabFrameAutoPaste;
            _settings.IsEditTextWindowEnabled = IsEditTextWindowEnabled;
            _settings.EditTextWindowWordWrap = EditTextWindowWordWrap;
            _settings.EditTextWindowAlwaysOnTop = EditTextWindowAlwaysOnTop;

            _settings.Save();

            // Shell integration for File Locksmith
            if (_fileLocksmithService != null)
            {
                _fileLocksmithService.SetContextMenuRegistered(IsFileLocksmithShellIntegrationEnabled);
            }

            // Broadcast tools settings changed message
            WeakReferenceMessenger.Default.Send(new ToolsSettingsChangedMessage(IsMmcLookupEnabled, IsAdminCommandsEnabled, IsShortcutGuideEnabled, IsGrabFrameEnabled, IsEditTextWindowEnabled));

            Sol.Helpers.Strings.CurrentLanguage = AppLanguage;
            
            // Notify MainWindow and workspaces to sync JIRA navigation in real-time
            WeakReferenceMessenger.Default.Send(new JiraSettingsChangedMessage(IsJiraEnabled));

            // Persist secrets securely to Windows Credential Locker without blocking general settings
            bool credentialError = false;
            string credentialErrorMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(JiraPatSecret))
            {
                try
                {
                    JiraCredentialHelper.SaveSecret("DataCenter", JiraPatSecret);
                }
                catch (Exception ex)
                {
                    credentialError = true;
                    credentialErrorMessage = ex.Message;
                }
            }
            else
            {
                try
                {
                    JiraCredentialHelper.ClearSecret("DataCenter");
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(JiraCloudTokenSecret))
            {
                try
                {
                    JiraCredentialHelper.SaveSecret("Cloud", JiraCloudTokenSecret);
                }
                catch (Exception ex)
                {
                    credentialError = true;
                    credentialErrorMessage = string.IsNullOrEmpty(credentialErrorMessage) ? ex.Message : $"{credentialErrorMessage}; {ex.Message}";
                }
            }
            else
            {
                try
                {
                    JiraCredentialHelper.ClearSecret("Cloud");
                }
                catch { }
            }

            if (credentialError)
            {
                WeakReferenceMessenger.Default.Send(
                    new AppNotificationMessage($"{Strings.S.SettingsSavedCredentialFailed}: {credentialErrorMessage}", InfoBarSeverity.Warning)
                );
            }
            else
            {
                WeakReferenceMessenger.Default.Send(
                    new AppNotificationMessage(Strings.S.SettingsSavedPrompt, InfoBarSeverity.Success)
                );
            }
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage($"{Strings.S.SettingsSaveErrorPrompt} ({ex.Message})", InfoBarSeverity.Error)
            );
        }
    }
}