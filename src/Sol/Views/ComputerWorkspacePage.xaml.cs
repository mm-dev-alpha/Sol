using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;
using Sol.ViewModels;

namespace Sol.Views;

public sealed partial class ComputerWorkspacePage : Page
{
    public ComputerWorkspaceViewModel ViewModel { get; }
    public Strings S => Strings.S;

    public ComputerWorkspacePage()
    {
        ViewModel = App.GetService<ComputerWorkspaceViewModel>();
        InitializeComponent();
    }

    private async void CenterSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            await ViewModel.SearchCenterCommand.ExecuteAsync(sender.Text);
        }
    }

    private async void CenterSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AdComputer chosenComp)
        {
            await ViewModel.LoadComputerAsync(chosenComp);
        }
    }

    private async void CenterSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is AdComputer chosenComp)
        {
            await ViewModel.LoadComputerAsync(chosenComp);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            await ViewModel.SearchAndLoadComputerAsync(args.QueryText);
        }
    }

    private async void MatchesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is AdComputer selectedComp)
        {
            await ViewModel.LoadComputerAsync(selectedComp);
        }
    }

    private void GroupFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            ViewModel.FilterGroupsCommand.Execute(tb.Text);
        }
    }

    private void AddGroupFlyout_Click(object sender, RoutedEventArgs e)
    {
        // Handled in XAML Flyout
    }

    private async void ConfirmAddGroup_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ViewModel.NewGroupName))
        {
            await ViewModel.AddToGroupCommand.ExecuteAsync(ViewModel.NewGroupName);
            AddGroupFlyout?.Hide();
        }
    }

    private async void RemoveGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string groupName)
        {
            await ViewModel.RemoveFromGroupCommand.ExecuteAsync(groupName);
        }
    }

    private void CopyText_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string text)
        {
            ViewModel.CopyToClipboardCommand.Execute(text);
        }
    }

    private void CopyBitLockerKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string pwd)
        {
            ViewModel.CopyBitLockerKeyCommand.Execute(pwd);
        }
    }

    private void CopyPs_GetAdComputer(object sender, RoutedEventArgs e) => ViewModel.CopyPowerShellCommand("Get-ADComputer");
    private void CopyPs_TestConnection(object sender, RoutedEventArgs e) => ViewModel.CopyPowerShellCommand("Test-Connection");
    private void CopyPs_EnterPsSession(object sender, RoutedEventArgs e) => ViewModel.CopyPowerShellCommand("Enter-PSSession");
    private void CopyPs_Mstsc(object sender, RoutedEventArgs e) => ViewModel.CopyPowerShellCommand("mstsc");

    private void Ping_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentComputer == null) return;
        string target = !string.IsNullOrWhiteSpace(ViewModel.CurrentComputer.DnsHostName) ? ViewModel.CurrentComputer.DnsHostName : ViewModel.CurrentComputer.Name;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k ping {target}",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore failure to start external process
        }
    }

    private void Rdp_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentComputer == null) return;
        string target = !string.IsNullOrWhiteSpace(ViewModel.CurrentComputer.DnsHostName) ? ViewModel.CurrentComputer.DnsHostName : ViewModel.CurrentComputer.Name;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "mstsc.exe",
                Arguments = $"/v:{target}",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore failure to start external process
        }
    }
}