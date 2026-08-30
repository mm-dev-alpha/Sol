using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sol.Helpers;
using Sol.Models;
using Sol.ViewModels;

namespace Sol.Views;

public sealed partial class ProcessManagerWindow : Window
{
    public ComputerWorkspaceViewModel ViewModel { get; }
    public Strings S => Strings.S;

    public ProcessManagerWindow(ComputerWorkspaceViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        string computerName = ViewModel.CurrentComputer?.Name ?? "Computer";
        Title = $"{S.ProcessManagerTitle} — {computerName}";
        TitleTextBlock.Text = $"{S.ProcessManagerTitle} — {computerName}";

        // Center on active screen and set default window size
        CenterAndResizeWindow();

        // Subscribe to close lifecycle requests
        ViewModel.CloseProcessManagerRequested += ViewModel_CloseProcessManagerRequested;
        this.Closed += ProcessManagerWindow_Closed;

        UpdateSortIndicators();

        // Fetch processes on launch
        _ = ViewModel.RefreshProcessesCommand.ExecuteAsync(null);
    }

    private void CenterAndResizeWindow()
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                int width = 960;
                int height = 680;
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
        catch
        {
            // Non-critical fallback
        }
    }

    private void ViewModel_CloseProcessManagerRequested()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                this.Close();
            }
            catch { }
        });
    }

    private void ProcessManagerWindow_Closed(object sender, WindowEventArgs args)
    {
        ViewModel.CloseProcessManagerRequested -= ViewModel_CloseProcessManagerRequested;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.FilterProcessesCommand.Execute(SearchBox.Text);
    }

    private bool _isDialogOpen;

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.RefreshProcessesCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            ViewModel.ShowError(ex.Message);
        }
    }

    private void SortColumn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string column)
        {
            ViewModel.ToggleProcessSortCommand.Execute(column);
            UpdateSortIndicators();
        }
    }

    private void UpdateSortIndicators()
    {
        SortIcon_PID.Text = string.Equals(ViewModel.ProcessSortColumn, "PID", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ProcessSortAscending ? "▲" : "▼") : "";
        SortIcon_Name.Text = string.Equals(ViewModel.ProcessSortColumn, "Name", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ProcessSortAscending ? "▲" : "▼") : "";
        SortIcon_User.Text = string.Equals(ViewModel.ProcessSortColumn, "User", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ProcessSortAscending ? "▲" : "▼") : "";
        SortIcon_CPU.Text = string.Equals(ViewModel.ProcessSortColumn, "CPU", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ProcessSortAscending ? "▲" : "▼") : "";
        SortIcon_Memory.Text = string.Equals(ViewModel.ProcessSortColumn, "Memory", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ProcessSortAscending ? "▲" : "▼") : "";
        SortIcon_Network.Text = string.Equals(ViewModel.ProcessSortColumn, "Network", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ProcessSortAscending ? "▲" : "▼") : "";
    }

    private async void TerminateProcess_Click(object sender, RoutedEventArgs e)
    {
        if (_isDialogOpen) return;
        if (sender is Button btn && btn.Tag is ComputerProcessInfo process)
        {
            if (!process.CanTerminate) return;

            _isDialogOpen = true;
            try
            {
                string host = ViewModel.CurrentComputer?.SamAccountName ?? string.Empty;
                var confirmDialog = new ContentDialog
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = S.ConfirmTerminateProcessTitle,
                    Content = Strings.ConfirmTerminateProcessPrompt(process.Name, process.ProcessId, host),
                    PrimaryButtonText = S.ConfirmBtn,
                    CloseButtonText = S.CancelBtn,
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.TerminateProcessCommand.ExecuteAsync(process);
                }
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
            }
            finally
            {
                _isDialogOpen = false;
            }
        }
    }
}
