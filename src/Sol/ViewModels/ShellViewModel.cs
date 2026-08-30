using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Sol.Models;
using Sol.Services;

namespace Sol.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    public UserWorkspaceViewModel UserWorkspace { get; }
    public GlobalSearchViewModel Search { get; }

    [ObservableProperty]
    public partial bool IsJiraNavVisible { get; set; }

    public Visibility JiraNavVisibility => IsJiraNavVisible ? Visibility.Visible : Visibility.Collapsed;

    public ShellViewModel(UserWorkspaceViewModel userWorkspace, GlobalSearchViewModel search, ISettingsService settings)
    {
        UserWorkspace = userWorkspace;
        Search = search;
        _settings = settings;

        IsJiraNavVisible = _settings.IsJiraEnabled;

        WeakReferenceMessenger.Default.Register<ShellViewModel, JiraSettingsChangedMessage>(this, static (r, m) =>
        {
            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                r.IsJiraNavVisible = m.IsEnabled;
                r.OnPropertyChanged(nameof(r.JiraNavVisibility));
            });
        });
    }
}
