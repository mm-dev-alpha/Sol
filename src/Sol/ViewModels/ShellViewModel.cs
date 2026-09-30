using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Sol.Models;
using Sol.Services;

namespace Sol.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    public GlobalSearchViewModel Search { get; }

    [ObservableProperty]
    public partial bool IsJiraNavVisible { get; set; }

    public Visibility JiraNavVisibility => IsJiraNavVisible ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    public partial bool IsRdsNavVisible { get; set; }

    public Visibility RdsNavVisibility => IsRdsNavVisible ? Visibility.Visible : Visibility.Collapsed;

    public ShellViewModel(GlobalSearchViewModel search, ISettingsService settings)
    {
        Search = search;
        _settings = settings;

        IsJiraNavVisible = _settings.IsJiraEnabled;
        IsRdsNavVisible = _settings.IsRdsEnabled;

        WeakReferenceMessenger.Default.Register<ShellViewModel, JiraSettingsChangedMessage>(this, static (r, m) =>
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                r.IsJiraNavVisible = m.IsEnabled;
                r.OnPropertyChanged(nameof(r.JiraNavVisibility));
            });
        });

        WeakReferenceMessenger.Default.Register<ShellViewModel, RdsSettingsChangedMessage>(this, static (r, m) =>
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                r.IsRdsNavVisible = m.IsEnabled;
                r.OnPropertyChanged(nameof(r.RdsNavVisibility));
            });
        });
    }
}
