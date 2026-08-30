using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Services;
using Sol.Helpers;
using Sol.Models;

namespace Sol.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IJiraService _jiraService;

    [ObservableProperty] public partial string Version { get; set; } = "3.5.0";
    [ObservableProperty] public partial string AdDomain { get; set; } = string.Empty;
    [ObservableProperty] public partial string AppLanguage { get; set; } = "en";
    [ObservableProperty] public partial int AppLanguageIndex { get; set; }

    // JIRA Integration
    [ObservableProperty] public partial bool IsJiraEnabled { get; set; }
    [ObservableProperty] public partial string JiraDeploymentMode { get; set; } = "DataCenter";
    [ObservableProperty] public partial int JiraDeploymentModeIndex { get; set; }
    [ObservableProperty] public partial string JiraBaseUrl { get; set; } = string.Empty;
    [ObservableProperty] public partial string JiraCloudEmail { get; set; } = string.Empty;
    [ObservableProperty] public partial string JiraSecret { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsTestingJira { get; set; }

    public bool IsJiraDataCenter => string.Equals(JiraDeploymentMode, "DataCenter", StringComparison.OrdinalIgnoreCase);
    public bool IsJiraCloud => string.Equals(JiraDeploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase);

    public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

    public SettingsViewModel(ISettingsService settings, IJiraService jiraService)
    {
        _settings = settings;
        _jiraService = jiraService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        AdDomain = _settings.AdDomain;
        AppLanguage = _settings.AppLanguage;
        AppLanguageIndex = string.Equals(AppLanguage, "de", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        IsJiraEnabled = _settings.IsJiraEnabled;
        JiraDeploymentMode = _settings.JiraDeploymentMode ?? "DataCenter";
        JiraDeploymentModeIndex = string.Equals(JiraDeploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        JiraBaseUrl = _settings.JiraBaseUrl ?? string.Empty;
        JiraCloudEmail = _settings.JiraCloudEmail ?? string.Empty;

        // Load secret from Windows Credential Locker (never stored in JSON)
        JiraSecret = JiraCredentialHelper.GetSecret(JiraDeploymentMode);
    }

    partial void OnAppLanguageIndexChanged(int value)
    {
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

    partial void OnJiraDeploymentModeIndexChanged(int value)
    {
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
        // Reload mode-specific secret from credential locker
        JiraSecret = JiraCredentialHelper.GetSecret(value);
    }

    [RelayCommand]
    private async Task TestJiraConnectionAsync()
    {
        if (IsTestingJira) return;
        IsTestingJira = true;
        try
        {
            bool success = await _jiraService.TestConnectionAsync(
                overrideBaseUrl: JiraBaseUrl,
                overrideMode: JiraDeploymentMode,
                overrideEmail: JiraCloudEmail,
                overrideSecret: JiraSecret);

            if (success)
            {
                WeakReferenceMessenger.Default.Send(
                    new AppNotificationMessage(Strings.S.JiraConnectionSuccessPrompt, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success)
                );
            }
            else
            {
                WeakReferenceMessenger.Default.Send(
                    new AppNotificationMessage(Strings.S.JiraConnectionFailedPrompt, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error)
                );
            }
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage($"{Strings.S.JiraConnectionFailedPrompt} ({ex.Message})", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error)
            );
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

            // Audit-proof credential storage in Windows Credential Locker
            if (!string.IsNullOrWhiteSpace(JiraSecret))
            {
                JiraCredentialHelper.SaveSecret(JiraDeploymentMode, JiraSecret);
            }
            
            Sol.Helpers.Strings.CurrentLanguage = AppLanguage;
            
            // Notify MainWindow to toggle JIRA navigation item visibility
            WeakReferenceMessenger.Default.Send(new JiraSettingsChangedMessage(IsJiraEnabled));

            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage(Strings.S.SettingsSavedPrompt, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success)
            );
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(
                new AppNotificationMessage($"{Strings.S.SettingsSaveErrorPrompt} ({ex.Message})", Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error)
            );
        }
    }
}