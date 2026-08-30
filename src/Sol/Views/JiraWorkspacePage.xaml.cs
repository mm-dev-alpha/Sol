using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Sol.Helpers;
using Sol.Models;
using Sol.ViewModels;

namespace Sol.Views;

public sealed partial class JiraWorkspacePage : Page
{
    public JiraWorkspaceViewModel ViewModel { get; }
    public Strings S => Strings.S;

    public JiraWorkspacePage()
    {
        ViewModel = App.GetService<JiraWorkspaceViewModel>();
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter != null)
        {
            try
            {
                await ViewModel.InitializeWithUserAsync(e.Parameter);
            }
            catch { }
        }
    }

    private async void CenterSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            try
            {
                await ViewModel.UpdateCenterSearchSuggestionsAsync(sender.Text);
            }
            catch { }
        }
    }

    private async void CenterSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is AdUser chosenUser)
        {
            try
            {
                await ViewModel.LoadUserAsync(chosenUser);
            }
            catch { }
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            try
            {
                await ViewModel.InitializeWithUserAsync(args.QueryText);
            }
            catch { }
        }
    }

    private async void CenterSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AdUser chosenUser)
        {
            try
            {
                await ViewModel.LoadUserAsync(chosenUser);
            }
            catch { }
        }
    }
}
