using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;

namespace Sol.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IActiveDirectoryService _adService;
    private readonly INavigationService _navigationService;
    private readonly IGreetingService _greetingService;

    [ObservableProperty]
    public partial string StartupGreeting { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CenterUserSearchQuery { get; set; } = string.Empty;

    public ObservableCollection<AdUser> CenterUserSuggestions { get; } = [];

    [ObservableProperty]
    public partial string CenterComputerSearchQuery { get; set; } = string.Empty;

    public ObservableCollection<AdComputer> CenterComputerSuggestions { get; } = [];

    public HomeViewModel(IActiveDirectoryService adService, INavigationService navigationService, IGreetingService greetingService)
    {
        _adService = adService;
        _navigationService = navigationService;
        _greetingService = greetingService;
        
        StartupGreeting = _greetingService.GetStartupGreeting();
    }

    [RelayCommand]
    private async Task SearchUsersAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            CenterUserSuggestions.Clear();
            return;
        }

        try
        {
            var users = await _adService.SearchUsersAsync(query);
            CenterUserSuggestions.Clear();
            foreach (var user in users.Take(8))
            {
                CenterUserSuggestions.Add(user);
            }
        }
        catch
        {
            CenterUserSuggestions.Clear();
        }
    }

    [RelayCommand]
    private async Task SearchComputersAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            CenterComputerSuggestions.Clear();
            return;
        }

        try
        {
            var computers = await _adService.SearchComputersAsync(query);
            CenterComputerSuggestions.Clear();
            foreach (var comp in computers.Take(8))
            {
                CenterComputerSuggestions.Add(comp);
            }
        }
        catch
        {
            CenterComputerSuggestions.Clear();
        }
    }

    public void ResetQueries()
    {
        CenterUserSearchQuery = string.Empty;
        CenterComputerSearchQuery = string.Empty;
        CenterUserSuggestions.Clear();
        CenterComputerSuggestions.Clear();
    }
}