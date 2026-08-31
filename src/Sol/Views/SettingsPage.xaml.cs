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
        this.Loaded += (s, e) => ViewModel.LoadSettings();
    }

    private void JiraPatBox_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is PasswordBox pb && !string.IsNullOrEmpty(ViewModel.JiraPatSecret))
        {
            pb.Password = ViewModel.JiraPatSecret;
        }
    }

    private void JiraPatBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            ViewModel.JiraPatSecret = pb.Password;
        }
    }

    private void JiraApiTokenBox_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is PasswordBox pb && !string.IsNullOrEmpty(ViewModel.JiraCloudTokenSecret))
        {
            pb.Password = ViewModel.JiraCloudTokenSecret;
        }
    }

    private void JiraApiTokenBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            ViewModel.JiraCloudTokenSecret = pb.Password;
        }
    }
}
