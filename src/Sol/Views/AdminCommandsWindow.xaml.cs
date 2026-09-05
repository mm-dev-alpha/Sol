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

public sealed partial class AdminCommandsWindow : Window
{
    public static AdminCommandsWindow? CurrentInstance { get; private set; }

    private readonly IAdminCommandService _commandService;
    private readonly DateTime _openedAt = DateTime.UtcNow;
    private readonly DispatcherTimer _feedbackTimer = new() { Interval = TimeSpan.FromSeconds(2.5) };
    private bool _isLoaded;

    public Strings S => Strings.S;

    public AdminCommandsWindow(IAdminCommandService commandService)
    {
        AppLog.Write("AdminCommandsWindow constructor started");
        _commandService = commandService;

        InitializeComponent();

        CurrentInstance = this;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Title = S.AdminCommandsWindowLauncherTitle;

        CenterAndResizeWindow();

        _feedbackTimer.Tick += (s, e) =>
        {
            _feedbackTimer.Stop();
            FeedbackPill.Visibility = Visibility.Collapsed;
        };

        Closed += (s, e) =>
        {
            AppLog.Write("AdminCommandsWindow Closed event fired");
            _feedbackTimer.Stop();
            _commandService.FavoritesChanged -= OnFavoritesChanged;
            if (CurrentInstance == this)
            {
                CurrentInstance = null;
            }
        };

        Activated += (s, e) =>
        {
            AppLog.Write($"AdminCommandsWindow Activated event: State = {e.WindowActivationState}");
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                if ((DateTime.UtcNow - _openedAt).TotalMilliseconds > 400)
                {
                    AppLog.Write("AdminCommandsWindow: Closing due to Deactivated state!");
                    Close();
                }
            }
        };

        if (Content is FrameworkElement root)
        {
            root.KeyDown += Root_KeyDown;
        }

        _commandService.FavoritesChanged += OnFavoritesChanged;

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

    private AdminShellType GetSelectedShell()
    {
        if (ShellFilterCombo?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return tag switch
            {
                "PowerShell" => AdminShellType.PowerShell,
                "Cmd" => AdminShellType.Cmd,
                _ => AdminShellType.All
            };
        }
        return AdminShellType.All;
    }

    private AdminCommandCategory GetSelectedCategory()
    {
        if (CategoryFilterCombo?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return tag switch
            {
                "ActiveDirectory" => AdminCommandCategory.ActiveDirectory,
                "Networking" => AdminCommandCategory.Networking,
                "GroupPolicy" => AdminCommandCategory.GroupPolicy,
                "Diagnostics" => AdminCommandCategory.Diagnostics,
                "Security" => AdminCommandCategory.Security,
                "RemoteManagement" => AdminCommandCategory.RemoteManagement,
                "Storage" => AdminCommandCategory.Storage,
                _ => AdminCommandCategory.All
            };
        }
        return AdminCommandCategory.All;
    }

    private void OnFavoritesChanged(object? sender, EventArgs e)
    {
        RefreshList();
    }

    private void RefreshList()
    {
        if (!_isLoaded || CommandsListView == null || ShellFilterCombo == null || CategoryFilterCombo == null || SearchBox == null || ItemCountText == null || EmptyStatePanel == null)
        {
            return;
        }

        string query = SearchBox.Text;
        var shell = GetSelectedShell();
        var category = GetSelectedCategory();

        var results = _commandService.SearchCommands(query, category, shell);
        CommandsListView.ItemsSource = results;

        ItemCountText.Text = string.Format(S.AdminCommandsItemCountFormat, results.Count);

        bool hasItems = results.Count > 0;
        CommandsListView.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
        EmptyStatePanel.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;

        if (hasItems && CommandsListView.SelectedIndex < 0)
        {
            CommandsListView.SelectedIndex = 0;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded) return;
        RefreshList();
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            if (CommandsListView.Items.Count > 0)
            {
                CommandsListView.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }
        else if (e.Key == VirtualKey.Enter)
        {
            if (CommandsListView.SelectedItem is AdminCommandItem item)
            {
                ExecuteCopy(item, closeAfter: true);
                e.Handled = true;
            }
            else if (CommandsListView.Items.Count > 0 && CommandsListView.Items[0] is AdminCommandItem first)
            {
                ExecuteCopy(first, closeAfter: true);
                e.Handled = true;
            }
        }
    }

    private void CommandsListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Enter)
        {
            if (CommandsListView.SelectedItem is AdminCommandItem item)
            {
                ExecuteCopy(item, closeAfter: true);
                e.Handled = true;
            }
        }
        else if (e.Key == VirtualKey.C && Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            if (CommandsListView.SelectedItem is AdminCommandItem item)
            {
                ExecuteCopy(item, closeAfter: false);
                e.Handled = true;
            }
        }
        else if (e.Key == VirtualKey.Up && CommandsListView.SelectedIndex == 0)
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

    private void CommandsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AdminCommandItem item)
        {
            ExecuteCopy(item, closeAfter: true);
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: AdminCommandItem item })
        {
            ExecuteCopy(item, closeAfter: false);
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string id })
        {
            _commandService.ToggleFavorite(id);
            RefreshList();
        }
    }

    private void ExecuteCopy(AdminCommandItem item, bool closeAfter)
    {
        string command = _commandService.CopyCommand(item);
        if (!string.IsNullOrEmpty(command))
        {
            var data = new DataPackage();
            data.SetText(command);
            Clipboard.SetContent(data);

            if (closeAfter)
            {
                Close();
            }
            else
            {
                ShowFeedback(S.AdminCommandsCopied);
            }
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
