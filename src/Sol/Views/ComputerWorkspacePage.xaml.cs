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
            try
            {
                await ViewModel.SearchCenterCommand.ExecuteAsync(sender.Text);
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private async void CenterSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is AdComputer chosenComp)
        {
            try
            {
                await ViewModel.LoadComputerAsync(chosenComp);
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private async void CenterSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        try
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
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private async void MatchesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is AdComputer selectedComp)
        {
            try
            {
                await ViewModel.LoadComputerAsync(selectedComp);
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
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

    private async void GroupSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            try
            {
                await ViewModel.SearchGroupsCommand.ExecuteAsync(sender.Text);
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private void GroupSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string groupName)
        {
            ViewModel.NewGroupName = groupName;
        }
    }

    private async void GroupSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            try
            {
                await ViewModel.AddToGroupCommand.ExecuteAsync(args.QueryText);
                AddGroupFlyout?.Hide();
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private async void ConfirmAddGroup_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ViewModel.NewGroupName))
        {
            try
            {
                await ViewModel.AddToGroupCommand.ExecuteAsync(ViewModel.NewGroupName);
                AddGroupFlyout?.Hide();
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private async void RemoveGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string groupName && !string.IsNullOrWhiteSpace(groupName))
        {
            try
            {
                string computerName = ViewModel.CurrentComputer?.SamAccountName ?? string.Empty;
                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = S.ConfirmRemoveFromGroupTitle,
                    Content = Strings.ConfirmRemoveComputerFromGroupPrompt(computerName, groupName),
                    PrimaryButtonText = S.ConfirmBtn,
                    CloseButtonText = S.CancelBtn,
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.RemoveFromGroupCommand.ExecuteAsync(groupName);
                }
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private async void DisableComputer_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentComputer == null) return;
        try
        {
            string computerName = ViewModel.CurrentComputer.SamAccountName;
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.ConfirmDisableAccountTitle,
                Content = Strings.ConfirmDisableComputerAccountPrompt(computerName),
                PrimaryButtonText = S.ConfirmBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.ToggleComputerAccountCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private async void OpenSessionUser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string samAccountName && !string.IsNullOrWhiteSpace(samAccountName))
        {
            try
            {
                await ViewModel.NavigateToSessionUserCommand.ExecuteAsync(samAccountName);
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private async void DisconnectSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ComputerSessionInfo session)
        {
            try
            {
                string host = ViewModel.CurrentComputer?.SamAccountName ?? string.Empty;
                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = S.ConfirmDisconnectSessionTitle,
                    Content = Strings.ConfirmDisconnectSessionPrompt(session.EffectiveDisplayName, host),
                    PrimaryButtonText = S.ConfirmBtn,
                    CloseButtonText = S.CancelBtn,
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.DisconnectSessionCommand.ExecuteAsync(session);
                }
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
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

    private async void RemoteGpupdate_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentComputer == null) return;
        try
        {
            string host = !string.IsNullOrWhiteSpace(ViewModel.CurrentComputer.DnsHostName) 
                ? ViewModel.CurrentComputer.DnsHostName 
                : ViewModel.CurrentComputer.Name;

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.ConfirmRemoteGpupdateTitle,
                Content = Strings.ConfirmRemoteGpupdatePrompt(host),
                PrimaryButtonText = S.ConfirmBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.TriggerRemoteGpupdateCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private async void SuspendBitLocker_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentComputer == null) return;
        try
        {
            string host = !string.IsNullOrWhiteSpace(ViewModel.CurrentComputer.DnsHostName) 
                ? ViewModel.CurrentComputer.DnsHostName 
                : ViewModel.CurrentComputer.Name;
            string drive = ViewModel.BitLockerSnapshot?.DriveLetter ?? "C:";

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.ConfirmSuspendBitLockerTitle,
                Content = Strings.ConfirmSuspendBitLockerPrompt(host, drive),
                PrimaryButtonText = S.ConfirmBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.SuspendBitLockerProtectionCommand.ExecuteAsync((uint)1);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private async void ResumeBitLocker_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentComputer == null) return;
        try
        {
            string host = !string.IsNullOrWhiteSpace(ViewModel.CurrentComputer.DnsHostName) 
                ? ViewModel.CurrentComputer.DnsHostName 
                : ViewModel.CurrentComputer.Name;
            string drive = ViewModel.BitLockerSnapshot?.DriveLetter ?? "C:";

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.ConfirmResumeBitLockerTitle,
                Content = Strings.ConfirmResumeBitLockerPrompt(host, drive),
                PrimaryButtonText = S.ConfirmBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.ResumeBitLockerProtectionCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private ProcessManagerWindow? _processManagerWindow;
    private ServicesInspectorWindow? _servicesInspectorWindow;

    private void OpenProcessManager_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentComputer == null) return;

        if (_processManagerWindow != null)
        {
            _processManagerWindow.Activate();
            return;
        }

        _processManagerWindow = new ProcessManagerWindow(ViewModel);
        _processManagerWindow.Closed += (s, args) =>
        {
            _processManagerWindow = null;
        };
        _processManagerWindow.Activate();
    }

    private void OpenServicesInspector_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentComputer == null) return;

        if (_servicesInspectorWindow != null)
        {
            _servicesInspectorWindow.Activate();
            return;
        }

        _servicesInspectorWindow = new ServicesInspectorWindow(ViewModel);
        _servicesInspectorWindow.Closed += (s, args) =>
        {
            _servicesInspectorWindow = null;
        };
        _servicesInspectorWindow.Activate();
    }

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