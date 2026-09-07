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
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly DispatcherTimer _toastTimer;
    private IntPtr _hwnd = IntPtr.Zero;

    public MainWindow() : this(
        App.GetService<ShellViewModel>(),
        App.GetService<INavigationService>(),
        App.GetService<IGlobalHotkeyService>())
    {
    }

    public MainWindow(ShellViewModel viewModel, INavigationService navigationService, IGlobalHotkeyService hotkeyService)
    {
        ViewModel = viewModel;
        _navigationService = navigationService;
        _hotkeyService = hotkeyService;
        
        InitializeComponent();

        Title = "Sol";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        try
        {
            AppLog.Write("MainWindow constructor: Getting HWND");
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            AppLog.Write($"MainWindow constructor: HWND = {_hwnd}");
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
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

            if (_hwnd != IntPtr.Zero)
            {
                _hotkeyService.Initialize(_hwnd);
                _hotkeyService.MmcLookupTriggered += (s, e) => DispatcherQueue.TryEnqueue(OpenMmcLookup);
                _hotkeyService.AdminCommandsTriggered += (s, e) => DispatcherQueue.TryEnqueue(OpenAdminCommands);
                _hotkeyService.ShortcutGuideTriggered += (s, e) => DispatcherQueue.TryEnqueue(OpenShortcutGuide);
                _hotkeyService.GrabFrameTriggered += (s, e) => DispatcherQueue.TryEnqueue(OpenGrabFrame);
                _hotkeyService.EditTextTriggered += (s, e) => DispatcherQueue.TryEnqueue(() => OpenEditTextWindow());
                _hotkeyService.EditTextOpenRequested += (s, e) => DispatcherQueue.TryEnqueue(() => OpenEditTextWindow(e.InitialText, e.InitialTable));
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"MainWindow constructor exception: {ex}");
        }

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
                var infoBarSeverity = m.Severity switch
                {
                    AppNotificationSeverity.Informational => InfoBarSeverity.Informational,
                    AppNotificationSeverity.Warning => InfoBarSeverity.Warning,
                    AppNotificationSeverity.Error => InfoBarSeverity.Error,
                    _ => InfoBarSeverity.Success
                };
                GlobalInfoBar.Severity = infoBarSeverity;
                GlobalInfoBar.IsOpen = true;

                // Errors stay visible until the user explicitly dismisses them via the 'X' button.
                // Informational, Success, and Warning notifications auto-dismiss after 4 seconds.
                if (infoBarSeverity != InfoBarSeverity.Error)
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


    private void OpenMmcLookup()
    {
        try
        {
            AppLog.Write("MainWindow.OpenMmcLookup called");
            if (Views.MmcLookupWindow.CurrentInstance != null)
            {
                AppLog.Write("MainWindow.OpenMmcLookup: closing existing CurrentInstance");
                Views.MmcLookupWindow.CurrentInstance.Close();
                return;
            }

            var service = App.GetService<IMmcLookupService>();
            var win = new Views.MmcLookupWindow(service);
            AppLog.Write("MainWindow.OpenMmcLookup: activating window");
            win.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"MainWindow.OpenMmcLookup exception: {ex}");
        }
    }

    private void OpenAdminCommands()
    {
        try
        {
            AppLog.Write("MainWindow.OpenAdminCommands called");
            if (Views.AdminCommandsWindow.CurrentInstance != null)
            {
                AppLog.Write("MainWindow.OpenAdminCommands: closing existing CurrentInstance");
                Views.AdminCommandsWindow.CurrentInstance.Close();
                return;
            }

            var service = App.GetService<IAdminCommandService>();
            var win = new Views.AdminCommandsWindow(service);
            AppLog.Write("MainWindow.OpenAdminCommands: activating window");
            win.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"MainWindow.OpenAdminCommands exception: {ex}");
        }
    }

    private void OpenShortcutGuide()
    {
        try
        {
            AppLog.Write("MainWindow.OpenShortcutGuide called");
            if (Views.ShortcutGuideOverlayWindow.CurrentInstance != null)
            {
                AppLog.Write("MainWindow.OpenShortcutGuide: closing existing CurrentInstance");
                Views.ShortcutGuideOverlayWindow.CurrentInstance.Close();
                return;
            }

            var service = App.GetService<IShortcutGuideService>();
            var win = new Views.ShortcutGuideOverlayWindow(service);
            AppLog.Write("MainWindow.OpenShortcutGuide: activating window");
            win.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"MainWindow.OpenShortcutGuide exception: {ex}");
        }
    }

    private void OpenGrabFrame()
    {
        try
        {
            AppLog.Write("MainWindow.OpenGrabFrame called");
            if (Views.GrabFrameWindow.CurrentInstance != null)
            {
                AppLog.Write("MainWindow.OpenGrabFrame: activating existing CurrentInstance");
                Views.GrabFrameWindow.CurrentInstance.Activate();
                return;
            }

            var captureService = App.GetService<IScreenCaptureService>();
            var ocrService = App.GetService<IOcrService>();
            var grabFrameService = App.GetService<IGrabFrameService>();
            var editTextService = App.GetService<IEditTextService>();
            var settings = App.GetService<ISettingsService>();
            var win = new Views.GrabFrameWindow(captureService, ocrService, grabFrameService, editTextService, settings);
            AppLog.Write("MainWindow.OpenGrabFrame: activating window");
            win.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"MainWindow.OpenGrabFrame exception: {ex}");
        }
    }

    public void OpenEditTextWindow(string? text = null, EditTextTableDocument? table = null)
    {
        try
        {
            AppLog.Write("MainWindow.OpenEditTextWindow called");
            if (Views.EditTextWindow.CurrentInstance != null)
            {
                AppLog.Write("MainWindow.OpenEditTextWindow: activating existing CurrentInstance");
                if (text != null || table != null)
                {
                    Views.EditTextWindow.CurrentInstance.LoadContent(text, table);
                }
                Views.EditTextWindow.CurrentInstance.Activate();
                return;
            }

            var editTextService = App.GetService<IEditTextService>();
            var textTransformService = App.GetService<ITextTransformService>();
            var ocrService = App.GetService<IOcrService>();
            var settings = App.GetService<ISettingsService>();
            var win = new Views.EditTextWindow(editTextService, textTransformService, ocrService, settings, text, table);
            AppLog.Write("MainWindow.OpenEditTextWindow: activating window");
            win.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"MainWindow.OpenEditTextWindow exception: {ex}");
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        try
        {
            _hotkeyService.Dispose();
        }
        catch { }

        try
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
        catch { }

        try
        {
            var compVm = App.GetService<ComputerWorkspaceViewModel>();
            compVm.RequestCloseProcessManager();
        }
        catch { }

        Application.Current.Exit();
    }
}