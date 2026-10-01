using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Helpers;
using Sol.Models;
using Sol.ViewModels;

namespace Sol.Views;

public sealed partial class RdsWorkspacePage : Page
{
    public RdsWorkspaceViewModel ViewModel { get; }
    public Strings S => Strings.S;

    public RdsWorkspacePage()
    {
        ViewModel = App.GetService<RdsWorkspaceViewModel>();
        this.InitializeComponent();
        UpdateSortIndicators();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        try
        {
            await ViewModel.InitializeAsync();
            UpdateSortIndicators();
        }
        catch (Exception ex)
        {
            AppLog.Write($"RdsWorkspacePage.OnNavigatedTo exception: {ex.Message}");
        }
    }

    private async void TabSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem == ProfileDisksSelectorItem)
        {
            ViewModel.SelectedTabIndex = 1;
            SessionsContainer.Visibility = Visibility.Collapsed;
            ProfileDisksContainer.Visibility = Visibility.Visible;

            if (ViewModel.Collections.Count == 0)
            {
                await ViewModel.LoadCollectionsAsync();
            }
        }
        else
        {
            ViewModel.SelectedTabIndex = 0;
            SessionsContainer.Visibility = Visibility.Visible;
            ProfileDisksContainer.Visibility = Visibility.Collapsed;
        }
    }

    // --- Sessions Tab Actions ---
    private async void DisconnectSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = SessionsListView.SelectedItems.OfType<RdsSessionItem>().ToList();
        if (selected.Count == 0)
        {
            if (ViewModel.SelectedSession != null)
            {
                selected.Add(ViewModel.SelectedSession);
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new AppNotificationMessage(S.RdsSessionsNoSelection, InfoBarSeverity.Warning));
                return;
            }
        }

        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = S.RdsSessionsConfirmDisconnectTitle,
            Content = string.Format(S.RdsSessionsConfirmDisconnectMsg, selected.Count),
            PrimaryButtonText = S.RdsSessionsDisconnectBtn,
            CloseButtonText = S.CancelBtn,
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DisconnectSessionsAsync(selected);
        }
    }

    private async void LogoffSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = SessionsListView.SelectedItems.OfType<RdsSessionItem>().ToList();
        if (selected.Count == 0)
        {
            if (ViewModel.SelectedSession != null)
            {
                selected.Add(ViewModel.SelectedSession);
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new AppNotificationMessage(S.RdsSessionsNoSelection, InfoBarSeverity.Warning));
                return;
            }
        }

        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = S.RdsSessionsConfirmLogoffTitle,
            Content = string.Format(S.RdsSessionsConfirmLogoffMsg, selected.Count),
            PrimaryButtonText = S.RdsSessionsLogoffBtn,
            CloseButtonText = S.CancelBtn,
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.LogoffSessionsAsync(selected);
        }
    }

    private async void RowDisconnect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RdsSessionItem session })
        {
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.RdsSessionsConfirmDisconnectTitle,
                Content = string.Format(S.RdsSessionsConfirmDisconnectMsg, 1),
                PrimaryButtonText = S.RdsSessionsDisconnectBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Close
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DisconnectSessionsAsync(new[] { session });
            }
        }
    }

    private async void RowLogoff_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RdsSessionItem session })
        {
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.RdsSessionsConfirmLogoffTitle,
                Content = string.Format(S.RdsSessionsConfirmLogoffMsg, 1),
                PrimaryButtonText = S.RdsSessionsLogoffBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Close
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.LogoffSessionsAsync(new[] { session });
            }
        }
    }

    private async void SessionsListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel.SelectedSession is { } s)
        {
            var stack = new StackPanel { Spacing = 8, Margin = new Thickness(0, 8, 0, 8) };
            stack.Children.Add(new TextBlock { Text = $"{S.RdsSessionsColUsername}: {s.Username}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            stack.Children.Add(new TextBlock { Text = $"{S.RdsSessionsColDomain}: {s.Domain}" });
            stack.Children.Add(new TextBlock { Text = $"{S.RdsSessionsColServer}: {s.HostServer}" });
            stack.Children.Add(new TextBlock { Text = $"{S.RdsSessionsColSessionId}: {s.SessionId} ({S.RdsUnifiedSessionIdLabel}: {s.UnifiedSessionId})" });
            stack.Children.Add(new TextBlock { Text = $"{S.RdsSessionsColState}: {s.DisplayState}" });
            stack.Children.Add(new TextBlock { Text = $"{S.RdsSessionsColLogonTime}: {s.FormattedLogonTime}" });

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.RdsSessionDetailsTitle,
                Content = stack,
                CloseButtonText = S.RdsSessionDetailsClose,
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }
    }

    private void SortColumn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string column)
        {
            ViewModel.ToggleSessionSortCommand.Execute(column);
            UpdateSortIndicators();
        }
    }

    private void UpdateSortIndicators()
    {
        SortIcon_Username.Text = string.Equals(ViewModel.SessionSortColumn, "Username", StringComparison.OrdinalIgnoreCase)
            ? (ViewModel.SessionSortAscending ? "▲" : "▼") : string.Empty;
        SortIcon_State.Text = string.Equals(ViewModel.SessionSortColumn, "State", StringComparison.OrdinalIgnoreCase)
            ? (ViewModel.SessionSortAscending ? "▲" : "▼") : string.Empty;
        SortIcon_HostServer.Text = string.Equals(ViewModel.SessionSortColumn, "HostServer", StringComparison.OrdinalIgnoreCase)
            ? (ViewModel.SessionSortAscending ? "▲" : "▼") : string.Empty;
        SortIcon_SessionId.Text = string.Equals(ViewModel.SessionSortColumn, "SessionId", StringComparison.OrdinalIgnoreCase)
            ? (ViewModel.SessionSortAscending ? "▲" : "▼") : string.Empty;
        SortIcon_LogonTime.Text = string.Equals(ViewModel.SessionSortColumn, "LogonTime", StringComparison.OrdinalIgnoreCase)
            ? (ViewModel.SessionSortAscending ? "▲" : "▼") : string.Empty;
    }

    // --- Profile Disks (UPD) Tab Actions ---
    private async void UserSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            try
            {
                await ViewModel.UpdateUserSearchSuggestionsAsync(sender.Text);
            }
            catch { }
        }
    }

    private async void UserSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AdUser chosenUser)
        {
            try
            {
                await ViewModel.SelectUserAsync(chosenUser);
            }
            catch { }
        }
    }

    private async void UserSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is AdUser user)
        {
            await ViewModel.SelectUserAsync(user);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            await ViewModel.SearchAndSelectFirstUserAsync(args.QueryText);
        }
    }

    public Visibility GetNoCollectionsWarningVisibility(int collectionsCount, bool isLoadingCollections, bool hasCollectionsError)
    {
        return collectionsCount == 0 && !isLoadingCollections && !hasCollectionsError ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetUpdNotEnabledWarningVisibility(RdsCollectionInfo? selectedCollection)
    {
        return selectedCollection != null && !selectedCollection.UpdEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void ExpandDisk_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.DiskLayout == null || ViewModel.SelectedUser == null) return;

        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = S.RdsUpdConfirmExpandTitle,
            Content = string.Format(S.RdsUpdConfirmExpandMsg, ViewModel.SelectedUser.DisplayName, ViewModel.DiskLayout.CapacityGiB, ViewModel.TargetGiB),
            PrimaryButtonText = S.RdsUpdExpandBtn,
            CloseButtonText = S.CancelBtn,
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ExpandDiskAsync();
        }
    }
}
