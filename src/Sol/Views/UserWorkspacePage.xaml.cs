using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sol.Helpers;
using Sol.Services;
using Sol.ViewModels;

namespace Sol.Views;

public sealed partial class UserWorkspacePage : Page
{
    public UserWorkspaceViewModel ViewModel { get; }
    public Strings S => Strings.S;

    public UserWorkspacePage()
    {
        ViewModel = App.GetService<UserWorkspaceViewModel>();
        InitializeComponent();
    }

    private void ManagerSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchManagerCommand.Execute(sender.Text);
        }
    }

    private void ManagerSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        ViewModel.ManagerSelectedCommand.Execute(args);
    }

    private void AttributeFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            ViewModel.FilterAttributesCommand.Execute(tb.Text);
        }
    }

    private void GroupFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            ViewModel.FilterGroupsCommand.Execute(tb.Text);
        }
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
        if (args.SelectedItem is Sol.Models.AdUser chosenUser)
        {
            try
            {
                await ViewModel.LoadUserAsync(chosenUser.SamAccountName);
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
            if (args.ChosenSuggestion is Sol.Models.AdUser chosenUser)
            {
                await ViewModel.LoadUserAsync(chosenUser.SamAccountName);
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                await ViewModel.LoadUserAsync(args.QueryText);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
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
                ViewModel.NewGroupName = args.QueryText;
                await ViewModel.AddToGroupCommand.ExecuteAsync(null);
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
                await ViewModel.AddToGroupCommand.ExecuteAsync(null);
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
                string userName = ViewModel.CurrentUser?.DisplayName ?? (ViewModel.CurrentUser?.SamAccountName ?? string.Empty);
                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = S.ConfirmRemoveFromGroupTitle,
                    Content = Strings.ConfirmRemoveUserFromGroupPrompt(userName, groupName),
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

    private async void DisableAccount_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentUser == null) return;
        try
        {
            string userName = ViewModel.CurrentUser.DisplayName;
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.ConfirmDisableAccountTitle,
                Content = Strings.ConfirmDisableUserAccountPrompt(userName),
                PrimaryButtonText = S.ConfirmBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DisableAccountCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private async void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentUser == null) return;
        try
        {
            string userName = ViewModel.CurrentUser.DisplayName;
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.ConfirmSaveProfileTitle,
                Content = Strings.ConfirmSaveProfilePrompt(userName),
                PrimaryButtonText = S.ConfirmBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.SaveProfileCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private async void DirectReport_Click(object sender, RoutedEventArgs e)
    {
        string? target = null;
        if (sender is FrameworkElement fe)
        {
            target = fe.Tag as string ?? fe.DataContext as string;
        }

        if (!string.IsNullOrWhiteSpace(target))
        {
            try
            {
                await ViewModel.NavigateToUserCommand.ExecuteAsync(target);
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private async void ManagerLink_Click(object sender, RoutedEventArgs e)
    {
        var manager = ViewModel.CurrentUser?.Manager;
        if (!string.IsNullOrWhiteSpace(manager))
        {
            try
            {
                await ViewModel.NavigateToUserCommand.ExecuteAsync(manager);
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private async void MatchesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is Sol.Models.AdUser selectedUser)
        {
            try
            {
                listView.SelectedItem = null;
                await ViewModel.LoadUserAsync(selectedUser.SamAccountName);
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }

    private async void ForcePasswordChange_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentUser == null) return;
        try
        {
            string userName = ViewModel.CurrentUser.DisplayName;
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = S.ConfirmForcePasswordChangeTitle,
                Content = Strings.ConfirmForcePasswordChangePrompt(userName),
                PrimaryButtonText = S.ConfirmBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.ForcePasswordChangeCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private async void ResetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentUser == null) return;

        try
        {
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = Strings.ResetPasswordDialogTitle(ViewModel.CurrentUser.DisplayName),
                PrimaryButtonText = S.ResetPasswordBtn,
                CloseButtonText = S.CancelBtn,
                DefaultButton = ContentDialogButton.Primary
            };

        var stack = new StackPanel { Spacing = 14 };

        var pwdHeader = new TextBlock 
        { 
            Text = S.NewPasswordLabel, 
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] 
        };
        stack.Children.Add(pwdHeader);

        var pwdGrid = new Grid { ColumnSpacing = 8 };
        pwdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pwdGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var passwordBox = new PasswordBox
        {
            PlaceholderText = S.NewPasswordPlaceholder,
            MinWidth = 280
        };
        Grid.SetColumn(passwordBox, 0);
        pwdGrid.Children.Add(passwordBox);

        var generateBtn = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new FontIcon { Glyph = "\uE8D7", FontSize = 12 },
                    new TextBlock { Text = S.GeneratePasswordBtn }
                }
            }
        };
        generateBtn.Click += (s, args) =>
        {
            var generated = UserWorkspaceViewModel.GenerateSecurePassword();
            passwordBox.Password = generated;
        };
        Grid.SetColumn(generateBtn, 1);
        pwdGrid.Children.Add(generateBtn);

        stack.Children.Add(pwdGrid);

        var mustChangeCheck = new CheckBox
        {
            Content = S.MustChangePasswordCheckbox,
            IsChecked = true
        };
        stack.Children.Add(mustChangeCheck);

        var unlockCheck = new CheckBox
        {
            Content = S.UnlockAccountCheckbox,
            IsChecked = ViewModel.CurrentUser.IsLockedOut,
            Visibility = ViewModel.CurrentUser.IsLockedOut ? Visibility.Visible : Visibility.Collapsed
        };
        stack.Children.Add(unlockCheck);

        var auditNote = new TextBlock
        {
            Text = S.PasswordResetAuditNotice,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(auditNote);

        dialog.Content = stack;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var newPwd = passwordBox.Password;
                if (!string.IsNullOrWhiteSpace(newPwd))
                {
                    await ViewModel.ResetPasswordWithPolicyCommand.ExecuteAsync(Tuple.Create(
                        newPwd,
                        mustChangeCheck.IsChecked ?? true,
                        unlockCheck.IsChecked ?? false
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private async void EditAttribute_Click(object sender, RoutedEventArgs e)
    {
        var item = (sender as Button)?.Tag as Sol.Models.AdAttributeItem 
                   ?? (sender as FrameworkElement)?.DataContext as Sol.Models.AdAttributeItem;
        if (item != null)
        {
            try
            {
                if (!ActiveDirectoryService.IsAttributeEditable(item.Key))
                {
                    var nonEditableDialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Title = S.AttributeEditorTitle,
                        Content = S.NonEditableAttributeTooltip,
                        CloseButtonText = S.CloseBtn,
                        DefaultButton = ContentDialogButton.Close
                    };
                    await nonEditableDialog.ShowAsync();
                    return;
                }

                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = S.ConfirmAttributeChangeTitle,
                    PrimaryButtonText = S.SaveBtn,
                    CloseButtonText = S.CancelBtn,
                    DefaultButton = ContentDialogButton.Primary
                };

                var stack = new StackPanel { Spacing = 12 };
                stack.Children.Add(new TextBlock 
                { 
                    Text = Strings.AttributeLabel(item.Key), 
                    Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] 
                });
                stack.Children.Add(new TextBlock 
                { 
                    Text = $"{S.OldValueLabel} {item.Value}", 
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] 
                });
                
                var input = new TextBox 
                { 
                    Text = item.Value, 
                    PlaceholderText = S.NewValueLabel, 
                    Header = S.NewValueLabel,
                    MinWidth = 360
                };
                stack.Children.Add(input);

                var note = new TextBlock 
                { 
                    Text = S.AuditLogNotice, 
                    Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                };
                stack.Children.Add(note);

                dialog.Content = stack;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        await ViewModel.CommitAttributeEditCommand.ExecuteAsync(Tuple.Create(item.Key, input.Text));
                    }
                    catch
                    {
                        // Error message handled in ViewModel & InfoBar
                    }
                }
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
        }
    }
}
