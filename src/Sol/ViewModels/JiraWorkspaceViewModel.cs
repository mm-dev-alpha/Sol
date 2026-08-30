using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;

namespace Sol.ViewModels;

public partial class JiraWorkspaceViewModel : ObservableObject
{
    private readonly IJiraService _jiraService;
    private readonly IActiveDirectoryService _adService;
    private readonly INavigationService _navigationService;

    public GlobalSearchViewModel Search { get; }

    [ObservableProperty]
    public partial AdUser? CurrentUser { get; set; }

    [ObservableProperty]
    public partial string FilterQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    [ObservableProperty]
    public partial bool HasMoreTickets { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CenterSearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<AdUser> CenterSuggestions { get; set; } = new();

    public ObservableCollection<JiraTicket> AllTickets { get; } = new();
    public ObservableCollection<JiraTicket> FilteredTickets { get; } = new();

    public bool HasUser => CurrentUser != null;
    public bool HasTickets => FilteredTickets.Count > 0;
    public bool HasNoTickets => !IsLoading && HasUser && FilteredTickets.Count == 0;
    public string TicketCountBadge => Strings.TotalJiraTicketsCountBadge(AllTickets.Count);

    public Visibility EmptyStateVisibility => CurrentUser == null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ContentVisibility => CurrentUser != null ? Visibility.Visible : Visibility.Collapsed;

    private int _currentStartAt = 0;
    private const int PageSize = 10;
    private CancellationTokenSource? _searchCts;

    public JiraWorkspaceViewModel(
        IJiraService jiraService,
        IActiveDirectoryService adService,
        GlobalSearchViewModel search,
        INavigationService navigationService)
    {
        _jiraService = jiraService;
        _adService = adService;
        Search = search;
        _navigationService = navigationService;
    }

    public async Task InitializeWithUserAsync(object? parameter)
    {
        if (parameter is AdUser user)
        {
            await LoadUserAsync(user);
        }
        else if (parameter is string identifier && !string.IsNullOrWhiteSpace(identifier))
        {
            try
            {
                var searchResults = await _adService.SearchUsersAsync(identifier);
                var matched = searchResults.FirstOrDefault(u => 
                    string.Equals(u.SamAccountName, identifier, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.Email, identifier, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.Upn, identifier, StringComparison.OrdinalIgnoreCase)) 
                    ?? searchResults.FirstOrDefault();

                if (matched != null)
                {
                    await LoadUserAsync(matched);
                }
                else
                {
                    // Fallback to minimal placeholder user
                    await LoadUserAsync(new AdUser
                    {
                        DisplayName = identifier,
                        SamAccountName = identifier,
                        Email = identifier.Contains("@") ? identifier : $"{identifier}@corp.local"
                    });
                }
            }
            catch
            {
                await LoadUserAsync(new AdUser
                {
                    DisplayName = identifier,
                    SamAccountName = identifier,
                    Email = identifier.Contains("@") ? identifier : $"{identifier}@corp.local"
                });
            }
        }
    }

    [RelayCommand]
    public async Task LoadUserAsync(AdUser user)
    {
        CurrentUser = user;
        CenterSearchQuery = string.Empty;
        CenterSuggestions.Clear();
        FilterQuery = string.Empty;
        HasError = false;
        ErrorMessage = string.Empty;
        _currentStartAt = 0;

        NotifyLayoutPropertiesChanged();
        await FetchTicketsAsync(isInitial: true);
    }

    [RelayCommand]
    public async Task RefreshTicketsAsync()
    {
        if (CurrentUser == null) return;
        _currentStartAt = 0;
        await FetchTicketsAsync(isInitial: true);
    }

    [RelayCommand]
    public async Task LoadMoreTicketsAsync()
    {
        if (CurrentUser == null || IsLoading || IsLoadingMore || !HasMoreTickets) return;
        _currentStartAt += PageSize;
        await FetchTicketsAsync(isInitial: false);
    }

    [RelayCommand]
    public void ResetToSearchState()
    {
        CurrentUser = null;
        AllTickets.Clear();
        FilteredTickets.Clear();
        FilterQuery = string.Empty;
        HasError = false;
        ErrorMessage = string.Empty;
        _currentStartAt = 0;
        HasMoreTickets = false;

        NotifyLayoutPropertiesChanged();
    }

    [RelayCommand]
    public void OpenTicketInBrowser(JiraTicket? ticket)
    {
        if (ticket == null || string.IsNullOrWhiteSpace(ticket.BrowseUrl)) return;
        try
        {
            var uri = new Uri(ticket.BrowseUrl);
            _ = Windows.System.Launcher.LaunchUriAsync(uri);
        }
        catch { }
    }

    public async Task UpdateCenterSearchSuggestionsAsync(string query)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            CenterSuggestions.Clear();
            return;
        }

        try
        {
            await Task.Delay(200, token);
            var results = await _adService.SearchUsersAsync(query);
            if (!token.IsCancellationRequested)
            {
                CenterSuggestions.Clear();
                foreach (var user in results.Take(8))
                {
                    CenterSuggestions.Add(user);
                }
            }
        }
        catch
        {
            // Ignore cancellation or search errors
        }
    }

    partial void OnFilterQueryChanged(string value)
    {
        ApplyFilter();
    }

    public void ApplyFilter()
    {
        FilteredTickets.Clear();
        string q = (FilterQuery ?? string.Empty).Trim();

        var queryable = string.IsNullOrWhiteSpace(q)
            ? AllTickets
            : AllTickets.Where(t => 
                t.Key.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                t.Summary.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                t.Status.Contains(q, StringComparison.OrdinalIgnoreCase));

        foreach (var ticket in queryable)
        {
            FilteredTickets.Add(ticket);
        }

        OnPropertyChanged(nameof(HasTickets));
        OnPropertyChanged(nameof(HasNoTickets));
    }

    private async Task FetchTicketsAsync(bool isInitial)
    {
        if (CurrentUser == null) return;

        if (isInitial)
        {
            IsLoading = true;
            AllTickets.Clear();
            FilteredTickets.Clear();
        }
        else
        {
            IsLoadingMore = true;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        NotifyLayoutPropertiesChanged();

        try
        {
            string lookupIdentifier = !string.IsNullOrWhiteSpace(CurrentUser.Email) 
                ? CurrentUser.Email 
                : (!string.IsNullOrWhiteSpace(CurrentUser.Upn) 
                    ? CurrentUser.Upn 
                    : CurrentUser.SamAccountName);

            var batch = await _jiraService.GetTicketsCreatedByUserAsync(lookupIdentifier, _currentStartAt, PageSize);

            foreach (var ticket in batch)
            {
                AllTickets.Add(ticket);
            }

            HasMoreTickets = batch.Count >= PageSize;
            ApplyFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            IsLoadingMore = false;
            NotifyLayoutPropertiesChanged();
        }
    }

    private void NotifyLayoutPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasUser));
        OnPropertyChanged(nameof(HasTickets));
        OnPropertyChanged(nameof(HasNoTickets));
        OnPropertyChanged(nameof(TicketCountBadge));
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(ContentVisibility));
    }
}
