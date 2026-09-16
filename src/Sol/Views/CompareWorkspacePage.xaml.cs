using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Sol.Helpers;
using Sol.Models;
using Sol.ViewModels;

namespace Sol.Views;

public sealed partial class CompareWorkspacePage : Page
{
    public CompareWorkspaceViewModel ViewModel { get; }
    public Strings S => Strings.S;
    private bool _isUpdatingModeSelector;

    public CompareWorkspacePage()
    {
        ViewModel = App.GetService<CompareWorkspaceViewModel>();
        _isUpdatingModeSelector = true;
        try
        {
            InitializeComponent();
        }
        finally
        {
            _isUpdatingModeSelector = false;
        }
        Loaded += CompareWorkspacePage_Loaded;
        Unloaded += CompareWorkspacePage_Unloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is InitiateComparisonMessage message)
        {
            AppLog.Write($"[COMPARE] CompareWorkspacePage.OnNavigatedTo: Mode={message.Mode}");
            ViewModel.HandleInitiateComparison(message);
            UpdateModeSelector();
            Bindings.Update();
            if (ViewModel.HasTargetA && !ViewModel.HasTargetB)
            {
                FocusSearchBoxB();
            }
        }
    }

    private void CompareWorkspacePage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdateModeSelector();
        Bindings.Update();
        if (ViewModel.HasTargetA && !ViewModel.HasTargetB)
        {
            FocusSearchBoxB();
        }
    }

    private void CompareWorkspacePage_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedMode))
        {
            UpdateModeSelector();
        }
    }

    private void FocusSearchBoxB()
    {
        if (SearchBoxB == null) return;

        void DoFocus()
        {
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (SearchBoxB != null && SearchBoxB.IsLoaded)
                {
                    bool focused = SearchBoxB.Focus(FocusState.Programmatic);
                    AppLog.Write($"[COMPARE] FocusSearchBoxB: Focus result = {focused}");
                }
            });
        }

        if (SearchBoxB.IsLoaded)
        {
            DoFocus();
        }
        else
        {
            RoutedEventHandler? handler = null;
            handler = (s, e) =>
            {
                SearchBoxB.Loaded -= handler;
                DoFocus();
            };
            SearchBoxB.Loaded += handler;
        }
    }

    private void UpdateModeSelector()
    {
        _isUpdatingModeSelector = true;
        try
        {
            if (ViewModel.IsUserMode)
            {
                if (!UsersSelectorItem.IsSelected)
                {
                    UsersSelectorItem.IsSelected = true;
                }
            }
            else
            {
                if (!ComputersSelectorItem.IsSelected)
                {
                    ComputersSelectorItem.IsSelected = true;
                }
            }
        }
        finally
        {
            _isUpdatingModeSelector = false;
        }
    }

    private async void SearchBoxA_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            try
            {
                await ViewModel.SearchAsync(sender.Text, isTargetA: true);
                sender.ItemsSource = ViewModel.SuggestionsA;
            }
            catch (Exception ex)
            {
                AppLog.Write($"SearchBoxA_TextChanged failed: {ex.Message}");
            }
        }
    }

    private async void SearchBoxB_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            try
            {
                await ViewModel.SearchAsync(sender.Text, isTargetA: false);
                sender.ItemsSource = ViewModel.SuggestionsB;
            }
            catch (Exception ex)
            {
                AppLog.Write($"SearchBoxB_TextChanged failed: {ex.Message}");
            }
        }
    }

    private void SearchBoxA_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is ComparisonSuggestionItem suggestion)
        {
            ViewModel.SelectSuggestion(suggestion, isTargetA: true);
            sender.Text = string.Empty;
        }
        else if (args.SelectedItem is AdUser user)
        {
            ViewModel.UserA = user;
            sender.Text = string.Empty;
        }
        else if (args.SelectedItem is AdComputer comp)
        {
            ViewModel.ComputerA = comp;
            sender.Text = string.Empty;
        }
    }

    private void SearchBoxB_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is ComparisonSuggestionItem suggestion)
        {
            ViewModel.SelectSuggestion(suggestion, isTargetA: false);
            sender.Text = string.Empty;
        }
        else if (args.SelectedItem is AdUser user)
        {
            ViewModel.UserB = user;
            sender.Text = string.Empty;
        }
        else if (args.SelectedItem is AdComputer comp)
        {
            ViewModel.ComputerB = comp;
            sender.Text = string.Empty;
        }
    }

    private void RemoveTargetA_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RemoveTargetACommand.Execute(null);
    }

    private void RemoveTargetB_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RemoveTargetBCommand.Execute(null);
    }

    private void ModeSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (_isUpdatingModeSelector) return;

        if (sender.SelectedItem == UsersSelectorItem && ViewModel.SelectedMode != ComparisonMode.Users)
        {
            ViewModel.SelectedMode = ComparisonMode.Users;
        }
        else if (sender.SelectedItem == ComputersSelectorItem && ViewModel.SelectedMode != ComparisonMode.Computers)
        {
            ViewModel.SelectedMode = ComparisonMode.Computers;
        }
    }

    private void GroupFilterSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem?.Tag is string tag)
        {
            ViewModel.GroupFilter = tag;
        }
    }
}
