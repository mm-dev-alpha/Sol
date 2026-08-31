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

    [ObservableProperty] public partial string Version { get; set; } = typeof(SettingsViewModel).Assembly.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "3.6.0";
    [ObservableProperty] public partial string AdDomain { get; set; } = string.Empty;
    [ObservableProperty] public partial string AppLanguage { get; set; } = "en";
    [ObservableProperty] public partial int AppLanguageIndex { get; set; }

    public string[] AvailableLanguages { get; } = ["English", "Deutsch"];
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotTestingJira))]
    public partial bool IsTestingJira { get; set; }
    [ObservableProperty] public partial bool IsJiraTestStatusOpen { get; set; }
    [ObservableProperty] public partial InfoBarSeverity JiraTestStatusSeverity { get; set; } = InfoBarSeverity.Informational;
    [ObservableProperty] public partial string JiraTestStatusMessage { get; set; } = string.Empty;

    public bool IsNotTestingJira => !IsTestingJira;
    public bool IsJiraDataCenter => string.Equals(JiraDeploymentMode, "DataCenter", StringComparison.OrdinalIgnoreCase);
    public bool IsJiraCloud => string.Equals(JiraDeploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase);

    public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "3.6.0.0";

    public SettingsViewModel(ISettingsService settings, IJiraService jiraService)
    {
        _settings = settings;
        _jiraService = jiraService;
        LoadSettings();
    }

    public void LoadSettings()
    {
        AdDomain = _settings.AdDomain;
        AppLanguage = _settings.AppLanguage;
        AppLanguageIndex = string.Equals(AppLanguage, "de", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        IsJiraEnabled = _settings.IsJiraEnabled;
        JiraDeploymentMode = _settings.JiraDeploymentMode ?? "DataCenter";
        JiraDeploymentModeIndex = string.Equals(JiraDeploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        JiraBaseUrl = _settings.JiraBaseUrl ?? string.Empty;
        JiraCloudEmail = _settings.JiraCloudEmail ?? string.Empty;

        // Load isolated secrets from Windows Credential Locker
        JiraPatSecret = JiraCredentialHelper.GetSecret("DataCenter");
        JiraCloudTokenSecret = JiraCredentialHelper.GetSecret("Cloud");

        OnPropertyChanged(nameof(IsJiraDataCenter));
        OnPropertyChanged(nameof(IsJiraCloud));
    }

    partial void OnAppLanguageIndexChanged(int value)
    {
        if (value < 0) return; // Ignore transient unselected index during ComboBox layout
        AppLanguage = value == 1 ? "de" : "en";
    }

    partial void OnAppLanguageChanged(string value)
    {
        int newIndex = string.Equals(value, "de", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        if (AppLanguageIndex != newIndex)
        {
            AppLanguageIndex = newIndex;
        }
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

        // Validation guard
        if (string.IsNullOrWhiteSpace(JiraBaseUrl))
        {
            JiraTestStatusMessage = Strings.S.JiraUrlRequiredPrompt;
            JiraTestStatusSeverity = InfoBarSeverity.Warning;
            IsJiraTestStatusOpen = true;
            return;
        }

        IsTestingJira = true;
        IsJiraTestStatusOpen = false;
        try
        {
            string secretToUse = IsJiraCloud ? JiraCloudTokenSecret : JiraPatSecret;
            bool success = await _jiraService.TestConnectionAsync(
                overrideBaseUrl: JiraBaseUrl,
                overrideMode: JiraDeploymentMode,
                overrideEmail: JiraCloudEmail,
                overrideSecret: secretToUse);

            if (success)
            {
                JiraTestStatusMessage = Strings.S.JiraConnectionSuccessPrompt;
                JiraTestStatusSeverity = InfoBarSeverity.Success;
                IsJiraTestStatusOpen = true;
            }
            else
            {
                JiraTestStatusMessage = Strings.S.JiraConnectionFailedPrompt;
                JiraTestStatusSeverity = InfoBarSeverity.Error;
                IsJiraTestStatusOpen = true;
            }
        }
        catch (Exception ex)
        {
            JiraTestStatusMessage = $"{Strings.S.JiraConnectionFailedPrompt}: {ex.Message}";
            JiraTestStatusSeverity = InfoBarSeverity.Error;
            IsJiraTestStatusOpen = true;
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
            _settings.Save();

            // Persist secrets securely to Windows Credential Locker
            if (!string.IsNullOrWhiteSpace(JiraPatSecret))
            {
                JiraCredentialHelper.SaveSecret("DataCenter", JiraPatSecret);
            }
            if (!string.IsNullOrWhiteSpace(JiraCloudTokenSecret))
            {
                JiraCredentialHelper.SaveSecret("Cloud", JiraCloudTokenSecret);
            }
            
            Sol.Helpers.Strings.CurrentLanguage = AppLanguage;
            
            // Notify MainWindow and workspaces to sync JIRA navigation in real-time
            WeakReferenceMessenger.Default.Send(new JiraSettingsChangedMessage(IsJiraEnabled));

            JiraTestStatusMessage = Strings.S.SettingsSavedPrompt;
            JiraTestStatusSeverity = InfoBarSeverity.Success;
            IsJiraTestStatusOpen = true;

            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage(Strings.S.SettingsSavedPrompt, InfoBarSeverity.Success)
            );
        }
        catch (Exception ex)
        {
            JiraTestStatusMessage = $"{Strings.S.SettingsSaveErrorPrompt} ({ex.Message})";
            JiraTestStatusSeverity = InfoBarSeverity.Error;
            IsJiraTestStatusOpen = true;

            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage($"{Strings.S.SettingsSaveErrorPrompt} ({ex.Message})", InfoBarSeverity.Error)
            );
        }
    }
}