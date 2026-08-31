using System;
using System.Linq;
using System.Security.Principal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;
using Sol.ViewModels;

namespace Sol;

public sealed partial class MainWindow : Window
{
    public ShellViewModel ViewModel { get; }
    public Strings S => Strings.S;
    private readonly INavigationService _navigationService;
    private readonly DispatcherTimer _toastTimer;

    public MainWindow()
    {
        ViewModel = App.GetService<ShellViewModel>();
        _navigationService = App.GetService<INavigationService>();
        
        InitializeComponent();

        Title = "Sol";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                appWindow.Title = "Sol";
                string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
            }
        }
        catch { }

        // Initialize DI NavigationService with Shell ContentFrame
        _navigationService.Initialize(ContentFrame);
        _navigationService.Navigated += OnNavigated;
        this.Closed += MainWindow_Closed;

        // Set Identity Context in TitleBar
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            RunningAsText.Text = $"{Strings.S.RunningAs}{identity.Name}";
            RunningAsPicture.DisplayName = identity.Name;
        }
        catch
        {
            RunningAsText.Text = $"{Strings.S.RunningAs}Unknown";
        }

        // Notification InfoBar timer & message subscription
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _toastTimer.Tick += (s, e) => { GlobalInfoBar.IsOpen = false; _toastTimer.Stop(); };

        // Register Global Notification Bus for In-App InfoBar Toasts
        WeakReferenceMessenger.Default.Register<AppNotificationMessage>(this, (r, m) =>
        {
            DispatcherQueue.TryEnqueue(() => 
            {
                _toastTimer.Stop();
                GlobalInfoBar.Message = m.Message;
                GlobalInfoBar.Severity = m.Severity;
                GlobalInfoBar.IsOpen = true;

                // Errors stay visible until the user explicitly dismisses them via the 'X' button.
                // Informational, Success, and Warning notifications auto-dismiss after 4 seconds.
                if (m.Severity != InfoBarSeverity.Error)
                {
                    _toastTimer.Start();
                }
            });
        });

        if (RootNavigationView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = Strings.S.NavSettings;
        }

        // Default initial navigation to Home
        RootNavigationView.SelectedItem = RootNavigationView.MenuItems.OfType<NavigationViewItem>().First();
        _navigationService.NavigateTo("HomePage");

        // Hook Splash Screen Dismissal once UI content has loaded
        if (Content is FrameworkElement rootElement)
        {
            rootElement.Loaded += MainWindow_Loaded;
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (RootNavigationView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = Strings.S.NavSettings;
        }

        // Smooth natural pause (~450ms) to ensure views and navigation are fully measured
        await System.Threading.Tasks.Task.Delay(450);

        try
        {
            if (SplashScreenFadeOutStoryboard != null)
            {
                SplashScreenFadeOutStoryboard.Completed += (s, args) =>
                {
                    SplashScreenOverlay.Visibility = Visibility.Collapsed;
                };
                SplashScreenFadeOutStoryboard.Begin();
            }
            else
            {
                SplashScreenOverlay.Visibility = Visibility.Collapsed;
            }
        }
        catch
        {
            SplashScreenOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void RootNavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            _navigationService.NavigateTo("SettingsPage");
        }
        else if (args.InvokedItemContainer?.Tag != null)
        {
            var pageTag = args.InvokedItemContainer.Tag.ToString()!;
            _navigationService.NavigateTo(pageTag);
        }
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            _navigationService.NavigateTo("SettingsPage");
        }
        else if (args.SelectedItemContainer?.Tag != null)
        {
            var pageTag = args.SelectedItemContainer.Tag.ToString()!;
            _navigationService.NavigateTo(pageTag);
        }
    }

    private void OnNavigated(object? sender, string pageKey)
    {
        // Dismiss active notifications on navigation between workspaces/pages
        GlobalInfoBar.IsOpen = false;
        _toastTimer.Stop();

        // Keep NavigationView selection synchronized with active page key
        if (pageKey == "SettingsPage")
        {
            RootNavigationView.SelectedItem = RootNavigationView.SettingsItem;
        }
        else
        {
            var item = RootNavigationView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (string?)i.Tag == pageKey);
            if (item != null)
            {
                RootNavigationView.SelectedItem = item;
            }
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        try
        {
            var compVm = App.GetService<ComputerWorkspaceViewModel>();
            compVm.RequestCloseProcessManager();
        }
        catch { }

        Application.Current.Exit();
    }
}