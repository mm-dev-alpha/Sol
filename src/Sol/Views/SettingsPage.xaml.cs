using Microsoft.UI.Xaml.Controls;
using Sol.ViewModels;

namespace Sol.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }
    public Sol.Helpers.Strings S => Sol.Helpers.Strings.S;

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }

    private void JiraSecretBox_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is PasswordBox pb && !string.IsNullOrEmpty(ViewModel.JiraSecret))
        {
            pb.Password = ViewModel.JiraSecret;
        }
    }

    private void JiraSecretBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            ViewModel.JiraSecret = pb.Password;
        }
    }
}
