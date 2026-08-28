using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Models;
using Sol.Services;

namespace Sol.ViewModels;

public partial class GlobalSearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public ObservableCollection<AdUser> Suggestions { get; } = new();

    public GlobalSearchViewModel(ISearchService searchService)
    {
        _searchService = searchService;
    }

    async partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Suggestions.Clear();
            return;
        }

        // Cancel any pending search
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            // Debounce delay
            await Task.Delay(300, token);

            if (token.IsCancellationRequested) return;

            IsLoading = true;
            var results = await _searchService.SearchUsersAsync(value, token);

            if (token.IsCancellationRequested) return;

            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                Suggestions.Clear();
                foreach (var user in results)
                {
                    Suggestions.Add(user);
                }
                IsLoading = false;
            });
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception)
        {
            // Optionally log error
            App.MainWindow?.DispatcherQueue.TryEnqueue(() => IsLoading = false);
        }
    }

    [RelayCommand]
    private void SuggestionChosen(AdUser chosenUser)
    {
        if (chosenUser != null)
        {
            // Broadcast the selection so MainWindow can navigate and Workspace can load
            WeakReferenceMessenger.Default.Send(new UserSearchSelectedMessage(chosenUser.SamAccountName));
            
            // Optional: clear query
            SearchQuery = string.Empty;
            Suggestions.Clear();
        }
    }
    
    [RelayCommand]
    private void QuerySubmitted(string queryText)
    {
        // If the user hits enter on a text query without selecting a suggestion,
        // we could pick the first one if it exists, or just send the query text.
        // If we send the query text, we can broadcast it so the main view handles multiple matches.
        if (!string.IsNullOrWhiteSpace(queryText))
        {
            WeakReferenceMessenger.Default.Send(new UserSearchSelectedMessage(queryText));
            SearchQuery = string.Empty;
            Suggestions.Clear();
        }
    }
}
