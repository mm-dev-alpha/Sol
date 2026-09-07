using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WinRT.Interop;

namespace Sol.Views;

public sealed partial class MmcLookupWindow : Window
{
    public static MmcLookupWindow? CurrentInstance { get; private set; }

    private readonly IMmcLookupService _mmcService;
    private readonly DateTime _openedAt = DateTime.UtcNow;
    private readonly DispatcherTimer _feedbackTimer = new() { Interval = TimeSpan.FromSeconds(2.5) };
    private bool _isLoaded;

    public Strings S => Strings.S;

    public MmcLookupWindow(IMmcLookupService mmcService)
    {
        AppLog.Write("MmcLookupWindow constructor started");
        _mmcService = mmcService;

        InitializeComponent();

        CurrentInstance = this;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Title = S.MmcWindowLauncherTitle;

        CenterAndResizeWindow();

        _feedbackTimer.Tick += (s, e) =>
        {
            _feedbackTimer.Stop();
            FeedbackPill.Visibility = Visibility.Collapsed;
        };

        Closed += (s, e) =>
        {
            AppLog.Write("MmcLookupWindow Closed event fired");
            _feedbackTimer.Stop();
            _mmcService.FavoritesChanged -= OnFavoritesChanged;
            if (CurrentInstance == this)
            {
                CurrentInstance = null;
            }
        };

        Activated += (s, e) =>
        {
            AppLog.Write($"MmcLookupWindow Activated event: State = {e.WindowActivationState}");
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                if ((DateTime.UtcNow - _openedAt).TotalMilliseconds > 400)
                {
                    AppLog.Write("MmcLookupWindow: Closing due to Deactivated state!");
                    Close();
                }
            }
        };

        if (Content is FrameworkElement root)
        {
            root.KeyDown += Root_KeyDown;
        }

        _mmcService.FavoritesChanged += OnFavoritesChanged;

        _isLoaded = true;
        RefreshList();

        // Focus search box automatically on launch
        SearchBox.Loaded += (s, e) => SearchBox.Focus(FocusState.Programmatic);
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
                int width = 880;
                int height = 580;
                int x = Math.Max(0, (displayArea.WorkArea.Width - width) / 2);
                int y = Math.Max(0, (displayArea.WorkArea.Height - height) / 2);
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));

                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
            }
        }
        catch { }
    }

    private MmcCategory GetSelectedCategory()
    {
        if (CategoryCombo?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return tag switch
            {
                "ActiveDirectory" => MmcCategory.ActiveDirectory,
                "Management" => MmcCategory.Management,
                "Diagnostics" => MmcCategory.Diagnostics,
                "Security" => MmcCategory.Security,
                "Networking" => MmcCategory.Networking,
                "Storage" => MmcCategory.Storage,
                "System" => MmcCategory.System,
                _ => MmcCategory.All
            };
        }
        return MmcCategory.All;
    }

    private void OnFavoritesChanged(object? sender, EventArgs e)
    {
        RefreshList();
    }

    private void RefreshList()
    {
        if (!_isLoaded || ToolsListView == null || CategoryCombo == null || SearchBox == null || ItemCountText == null || EmptyStatePanel == null)
        {
            return;
        }

        string query = SearchBox.Text;
        var category = GetSelectedCategory();

        var results = _mmcService.SearchTools(query, category);
        ToolsListView.ItemsSource = results;

        ItemCountText.Text = string.Format(S.MmcItemCountFormat, results.Count);

        bool hasItems = results.Count > 0;
        ToolsListView.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
        EmptyStatePanel.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;

        if (hasItems && ToolsListView.SelectedIndex < 0)
        {
            ToolsListView.SelectedIndex = 0;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded) return;
        RefreshList();
    }

    private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded) return;
        RefreshList();
    }

    private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Down)
        {
            if (ToolsListView.Items.Count > 0)
            {
                ToolsListView.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }
        else if (e.Key == VirtualKey.Enter)
        {
            if (ToolsListView.SelectedItem is MmcToolItem item)
            {
                ExecuteLaunch(item);
                e.Handled = true;
            }
            else if (ToolsListView.Items.Count > 0 && ToolsListView.Items[0] is MmcToolItem first)
            {
                ExecuteLaunch(first);
                e.Handled = true;
            }
        }
    }

    private void ToolsListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Enter)
        {
            if (ToolsListView.SelectedItem is MmcToolItem item)
            {
                ExecuteLaunch(item);
                e.Handled = true;
            }
        }
        else if (e.Key == VirtualKey.C && Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            if (ToolsListView.SelectedItem is MmcToolItem item)
            {
                ExecuteCopy(item);
                e.Handled = true;
            }
        }
        else if (e.Key == VirtualKey.Up && ToolsListView.SelectedIndex == 0)
        {
            SearchBox.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            Close();
        }
    }

    private void ToolsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is MmcToolItem item)
        {
            ExecuteLaunch(item);
        }
    }

    private void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: MmcToolItem item })
        {
            ExecuteLaunch(item);
        }
    }

    private void CopyCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: MmcToolItem item })
        {
            ExecuteCopy(item);
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string id })
        {
            _mmcService.ToggleFavorite(id);
            RefreshList();
        }
    }

    private void ExecuteLaunch(MmcToolItem item)
    {
        if (_mmcService.LaunchTool(item, out string? error))
        {
            Close();
        }
        else if (!string.IsNullOrEmpty(error))
        {
            ShowFeedback(error);
        }
    }

    private void ExecuteCopy(MmcToolItem item)
    {
        string command = _mmcService.CopyRunCommand(item);
        if (!string.IsNullOrEmpty(command))
        {
            if (SafeClipboard.TrySetText(command))
                ShowFeedback(S.MmcCopiedCommand);
            else
                ShowFeedback(S.ClipboardBusy);
        }
    }

    private void ShowFeedback(string message)
    {
        FeedbackText.Text = message;
        FeedbackPill.Visibility = Visibility.Visible;
        _feedbackTimer.Stop();
        _feedbackTimer.Start();
    }
}
