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

    private async void UserSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AdUser chosenUser)
        {
            var nav = App.GetService<INavigationService>();
            var userVm = App.GetService<UserWorkspaceViewModel>();
            await userVm.LoadUserAsync(chosenUser.SamAccountName);
            nav.NavigateTo("UserWorkspacePage");
        }
    }

    private async void UserSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var nav = App.GetService<INavigationService>();
        var userVm = App.GetService<UserWorkspaceViewModel>();

        if (args.ChosenSuggestion is AdUser chosenUser)
        {
            await userVm.LoadUserAsync(chosenUser.SamAccountName);
            nav.NavigateTo("UserWorkspacePage");
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            await userVm.LoadUserAsync(args.QueryText);
            nav.NavigateTo("UserWorkspacePage");
        }
    }

    private async void ComputerSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            await ViewModel.SearchComputersCommand.ExecuteAsync(sender.Text);
        }
    }

    private async void ComputerSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AdComputer chosenComp)
        {
            var nav = App.GetService<INavigationService>();
            var compVm = App.GetService<ComputerWorkspaceViewModel>();
            await compVm.LoadComputerAsync(chosenComp);
            nav.NavigateTo("ComputerWorkspacePage");
        }
    }

    private async void ComputerSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var nav = App.GetService<INavigationService>();
        var compVm = App.GetService<ComputerWorkspaceViewModel>();

        if (args.ChosenSuggestion is AdComputer chosenComp)
        {
            await compVm.LoadComputerAsync(chosenComp);
            nav.NavigateTo("ComputerWorkspacePage");
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            await compVm.SearchAndLoadComputerAsync(args.QueryText);
            nav.NavigateTo("ComputerWorkspacePage");
        }
    }
}