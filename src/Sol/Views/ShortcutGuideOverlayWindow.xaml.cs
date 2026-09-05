using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;
using Windows.System;
using WinRT.Interop;

namespace Sol.Views;

public sealed partial class ShortcutGuideOverlayWindow : Window
{
    public static ShortcutGuideOverlayWindow? CurrentInstance { get; private set; }
    private readonly IShortcutGuideService _shortcutService;
    private readonly DateTime _openedAt = DateTime.UtcNow;
    public Strings S => Strings.S;

    public ShortcutGuideOverlayWindow(IShortcutGuideService shortcutService)
    {
        AppLog.Write("ShortcutGuideOverlayWindow constructor started");
        CurrentInstance = this;
        _shortcutService = shortcutService;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        Title = S.ShortcutGuideTitle;

        CenterAndResizeWindow();

        LoadCategories();

        Closed += (s, e) =>
        {
            AppLog.Write("ShortcutGuideOverlayWindow Closed event fired");
            if (CurrentInstance == this)
            {
                CurrentInstance = null;
            }
        };

        Activated += (s, e) =>
        {
            AppLog.Write($"ShortcutGuideOverlayWindow Activated event: State = {e.WindowActivationState}");
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                if ((DateTime.UtcNow - _openedAt).TotalMilliseconds > 400)
                {
                    AppLog.Write("ShortcutGuideOverlayWindow: Closing due to Deactivated state!");
                    Close();
                }
                else
                {
                    AppLog.Write("ShortcutGuideOverlayWindow: Ignoring Deactivated during initial grace period");
                }
            }
        };

        SearchBox.KeyDown += (s, e) =>
        {
            if (e.Key == VirtualKey.Escape)
            {
                Close();
            }
        };

        if (Content is FrameworkElement root)
        {
            root.KeyDown += Root_KeyDown;
        }
    }

    private void CenterAndResizeWindow()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                int width = 980;
                int height = 660;
                int x = Math.Max(0, (displayArea.WorkArea.Width - width) / 2);
                int y = Math.Max(0, (displayArea.WorkArea.Height - height) / 2);
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));

                string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
            }
        }
        catch { }
    }

    private void LoadCategories()
    {
        CategoriesItemsControl.ItemsSource = _shortcutService.GetCategories();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text;
        if (string.IsNullOrWhiteSpace(query))
        {
            LoadCategories();
            return;
        }

        var matches = _shortcutService.SearchShortcuts(query);
        var grouped = matches
            .GroupBy(m => m.Category)
            .Select(g => new ShortcutCategory(g.Key, g.ToList()))
            .ToList();

        CategoriesItemsControl.ItemsSource = grouped;
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            Close();
        }
    }
}
