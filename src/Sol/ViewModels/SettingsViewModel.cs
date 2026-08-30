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

    [ObservableProperty] public partial string Version { get; set; } = "3.5.0";
    [ObservableProperty] public partial string AdDomain { get; set; } = string.Empty;
    [ObservableProperty] public partial string AppLanguage { get; set; } = "en";

    public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        LoadSettings();
    }

    private void LoadSettings()
    {
        AdDomain = _settings.AdDomain;
        AppLanguage = _settings.AppLanguage;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.AdDomain = AdDomain;
        _settings.AppLanguage = AppLanguage;
        _settings.Save();
        
        Sol.Helpers.Strings.CurrentLanguage = AppLanguage;
        
        WeakReferenceMessenger.Default.Send(
            new AppNotificationMessage(Strings.S.SettingsSavedPrompt, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success)
        );
    }
}