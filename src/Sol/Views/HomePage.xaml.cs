using Microsoft.UI.Xaml.Controls;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;
using Sol.ViewModels;

namespace Sol.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel => (HomeViewModel)DataContext;
    public Strings S => Strings.S;

    public HomePage()
    {
        this.InitializeComponent();
        DataContext = App.GetService<HomeViewModel>();
    }

    private async void UserSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            await ViewModel.SearchUsersCommand.ExecuteAsync(sender.Text);
        }
    }

    private void UserSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AdUser chosenUser)
        {
            var nav = App.GetService<INavigationService>();
            nav.NavigateTo("UserWorkspacePage", chosenUser.SamAccountName);
        }
    }

    private void UserSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var nav = App.GetService<INavigationService>();
        if (args.ChosenSuggestion is AdUser chosenUser)
        {
            nav.NavigateTo("UserWorkspacePage", chosenUser.SamAccountName);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            nav.NavigateTo("UserWorkspacePage", args.QueryText);
        }
    }

    private async void ComputerSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            await ViewModel.SearchComputersCommand.ExecuteAsync(sender.Text);
        }
    }

    private void ComputerSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AdComputer chosenComp)
        {
            var nav = App.GetService<INavigationService>();
            nav.NavigateTo("ComputerWorkspacePage", chosenComp);
        }
    }

    private void ComputerSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var nav = App.GetService<INavigationService>();
        if (args.ChosenSuggestion is AdComputer chosenComp)
        {
            nav.NavigateTo("ComputerWorkspacePage", chosenComp);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            nav.NavigateTo("ComputerWorkspacePage", args.QueryText);
        }
    }
}