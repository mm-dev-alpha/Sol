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
}
