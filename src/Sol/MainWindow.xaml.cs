using System;
using System.Linq;
using System.Runtime.InteropServices;
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
    private const uint WM_HOTKEY = 0x0312;
    private const int HOTKEY_MMC_LOOKUP_ID = 0x4D4D;
    private const int HOTKEY_ADMIN_COMMANDS_ID = 0x4143;
    private const int HOTKEY_SHORTCUT_GUIDE_ID = 0x5347;
    private const int HOTKEY_GRAB_FRAME_ID = 0x4746;
    private const int HOTKEY_EDIT_TEXT_ID = 0x5345;
    private const uint SUBCLASS_ID = 101;

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass, UIntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData);

    public ShellViewModel ViewModel { get; }
    public Strings S => Strings.S;
    private readonly INavigationService _navigationService;
    private readonly DispatcherTimer _toastTimer;
    private IntPtr _hwnd = IntPtr.Zero;
    private SubclassProc? _subclassProc;

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
                _subclassProc = new SubclassProc(WndProc);
                bool subclassResult = SetWindowSubclass(_hwnd, _subclassProc, (UIntPtr)SUBCLASS_ID, UIntPtr.Zero);
                AppLog.Write($"MainWindow constructor: SetWindowSubclass result = {subclassResult}, lastErr = {Marshal.GetLastWin32Error()}");

                var settings = App.GetService<ISettingsService>();
                UpdateGlobalHotkeys(settings.IsMmcLookupEnabled, settings.IsAdminCommandsEnabled, settings.IsShortcutGuideEnabled, settings.IsGrabFrameEnabled, settings.IsEditTextWindowEnabled);

                WeakReferenceMessenger.Default.Register<ToolsSettingsChangedMessage>(this, (r, m) =>
                {
                    UpdateGlobalHotkeys(m.IsMmcLookupEnabled, m.IsAdminCommandsEnabled, m.IsShortcutGuideEnabled, m.IsGrabFrameEnabled, m.IsEditTextWindowEnabled);
                });

                var mmcService = App.GetService<IMmcLookupService>();
                if (mmcService != null)
                {
                    mmcService.OpenRequested += (s, e) => DispatcherQueue.TryEnqueue(() => OpenMmcLookup());
                }

                var adminCommandsService = App.GetService<IAdminCommandService>();
                if (adminCommandsService != null)
                {
                    adminCommandsService.OpenRequested += (s, e) => DispatcherQueue.TryEnqueue(() => OpenAdminCommands());
                }

                var editTextService = App.GetService<IEditTextService>();
                if (editTextService != null)
                {
                    editTextService.ToggleRequested += (s, e) => DispatcherQueue.TryEnqueue(() => OpenEditTextWindow());
                    editTextService.OpenRequested += (s, e) => DispatcherQueue.TryEnqueue(() => OpenEditTextWindow(e.InitialText, e.InitialTable));
                }
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

    private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData)
    {
        if (uMsg == WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            AppLog.Write($"MainWindow.WndProc: WM_HOTKEY received! hotkeyId = 0x{hotkeyId:X4}");
            if (hotkeyId == HOTKEY_MMC_LOOKUP_ID)
            {
                DispatcherQueue.TryEnqueue(OpenMmcLookup);
                return IntPtr.Zero;
            }
            if (hotkeyId == HOTKEY_ADMIN_COMMANDS_ID)
            {
                DispatcherQueue.TryEnqueue(OpenAdminCommands);
                return IntPtr.Zero;
            }
            if (hotkeyId == HOTKEY_SHORTCUT_GUIDE_ID)
            {
                DispatcherQueue.TryEnqueue(OpenShortcutGuide);
                return IntPtr.Zero;
            }
            if (hotkeyId == HOTKEY_GRAB_FRAME_ID)
            {
                DispatcherQueue.TryEnqueue(OpenGrabFrame);
                return IntPtr.Zero;
            }
            if (hotkeyId == HOTKEY_EDIT_TEXT_ID)
            {
                DispatcherQueue.TryEnqueue(() => OpenEditTextWindow());
                return IntPtr.Zero;
            }
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
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

    private void UpdateGlobalHotkeys(bool mmcEnabled, bool adminCommandsEnabled, bool shortcutGuideEnabled, bool grabFrameEnabled = true, bool editTextEnabled = true)
    {
        if (_hwnd == IntPtr.Zero)
        {
            AppLog.Write("UpdateGlobalHotkeys: _hwnd is Zero!");
            return;
        }
        try
        {
            var mmcService = App.GetService<IMmcLookupService>();
            var adminCommandsService = App.GetService<IAdminCommandService>();
            var shortcutService = App.GetService<IShortcutGuideService>();
            var grabFrameService = App.GetService<IGrabFrameService>();
            var editTextService = App.GetService<IEditTextService>();

            if (mmcEnabled)
            {
                bool r = mmcService?.RegisterGlobalHotkey(_hwnd) ?? false;
                AppLog.Write($"UpdateGlobalHotkeys: MMC registered = {r}");
            }
            else mmcService?.UnregisterGlobalHotkey(_hwnd);

            if (adminCommandsEnabled)
            {
                bool r = adminCommandsService?.RegisterGlobalHotkey(_hwnd) ?? false;
                AppLog.Write($"UpdateGlobalHotkeys: AdminCommands registered = {r}");
            }
            else adminCommandsService?.UnregisterGlobalHotkey(_hwnd);

            if (shortcutGuideEnabled)
            {
                bool r = shortcutService?.RegisterGlobalHotkey(_hwnd) ?? false;
                AppLog.Write($"UpdateGlobalHotkeys: ShortcutGuide registered = {r}");
            }
            else shortcutService?.UnregisterGlobalHotkey(_hwnd);

            if (grabFrameEnabled)
            {
                bool r = grabFrameService?.RegisterGlobalHotkey(_hwnd) ?? false;
                AppLog.Write($"UpdateGlobalHotkeys: GrabFrame registered = {r}");
            }
            else grabFrameService?.UnregisterGlobalHotkey(_hwnd);

            if (editTextEnabled)
            {
                bool r = editTextService?.RegisterGlobalHotkey(_hwnd) ?? false;
                AppLog.Write($"UpdateGlobalHotkeys: EditText registered = {r}");
            }
            else editTextService?.UnregisterGlobalHotkey(_hwnd);
        }
        catch (Exception ex)
        {
            AppLog.Write($"UpdateGlobalHotkeys exception: {ex}");
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        try
        {
            if (_hwnd != IntPtr.Zero)
            {
                if (_subclassProc != null)
                {
                    RemoveWindowSubclass(_hwnd, _subclassProc, (UIntPtr)SUBCLASS_ID);
                }
                App.GetService<IMmcLookupService>()?.UnregisterGlobalHotkey(_hwnd);
                App.GetService<IMmcLookupService>()?.Dispose();
                App.GetService<IAdminCommandService>()?.UnregisterGlobalHotkey(_hwnd);
                App.GetService<IAdminCommandService>()?.Dispose();
                App.GetService<IShortcutGuideService>()?.UnregisterGlobalHotkey(_hwnd);
                App.GetService<IGrabFrameService>()?.UnregisterGlobalHotkey(_hwnd);
                App.GetService<IGrabFrameService>()?.Dispose();
                App.GetService<IEditTextService>()?.UnregisterGlobalHotkey(_hwnd);
                (App.GetService<IEditTextService>() as IDisposable)?.Dispose();
            }
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

        try
        {
            App.GetService<IAwakeService>()?.Dispose();
        }
        catch { }

        Application.Current.Exit();
    }
}