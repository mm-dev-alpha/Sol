using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sol.Helpers;
using Sol.Models;
using Sol.ViewModels;

namespace Sol.Views;

public sealed partial class ServicesInspectorWindow : Window
{
    public ComputerWorkspaceViewModel ViewModel { get; }
    public Strings S => Strings.S;

    public ServicesInspectorWindow(ComputerWorkspaceViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        string computerName = ViewModel.CurrentComputer?.Name ?? "Computer";
        Title = $"{S.ServicesInspectorTitle} — {computerName}";
        TitleTextBlock.Text = $"{S.ServicesInspectorTitle} — {computerName}";

        // Center on active screen and set default window size (1060x720)
        CenterAndResizeWindow();

        // Subscribe to close lifecycle requests
        ViewModel.CloseServicesInspectorRequested += ViewModel_CloseServicesInspectorRequested;
        this.Closed += ServicesInspectorWindow_Closed;

        UpdateSortIndicators();

        // Fetch services on launch
        _ = ViewModel.RefreshServicesCommand.ExecuteAsync(null);
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
                int width = 1060;
                int height = 720;
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

    private void ViewModel_CloseServicesInspectorRequested()
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

    private void ServicesInspectorWindow_Closed(object sender, WindowEventArgs args)
    {
        ViewModel.CloseServicesInspectorRequested -= ViewModel_CloseServicesInspectorRequested;
    }

    private void FilterRadio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string filterTag)
        {
            ViewModel.SetServiceStatusFilterCommand.Execute(filterTag);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.FilterServicesCommand.Execute(SearchBox.Text);
    }

    private bool _isDialogOpen;

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.RefreshServicesCommand.ExecuteAsync(null);
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
            ViewModel.ToggleServiceSortCommand.Execute(column);
            UpdateSortIndicators();
        }
    }

    private void UpdateSortIndicators()
    {
        SortIcon_DisplayName.Text = string.Equals(ViewModel.ServiceSortColumn, "DisplayName", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ServiceSortAscending ? "▲" : "▼") : "";
        SortIcon_Name.Text = string.Equals(ViewModel.ServiceSortColumn, "Name", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ServiceSortAscending ? "▲" : "▼") : "";
        SortIcon_Status.Text = string.Equals(ViewModel.ServiceSortColumn, "Status", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ServiceSortAscending ? "▲" : "▼") : "";
        SortIcon_StartMode.Text = string.Equals(ViewModel.ServiceSortColumn, "StartMode", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ServiceSortAscending ? "▲" : "▼") : "";
        SortIcon_StartName.Text = string.Equals(ViewModel.ServiceSortColumn, "StartName", StringComparison.OrdinalIgnoreCase) ? (ViewModel.ServiceSortAscending ? "▲" : "▼") : "";
    }

    private async void StartService_Click(object sender, RoutedEventArgs e)
    {
        if (_isDialogOpen) return;
        if (sender is Button btn && btn.Tag is ComputerServiceInfo service)
        {
            if (!service.CanStart) return;

            _isDialogOpen = true;
            try
            {
                string host = ViewModel.CurrentComputer?.SamAccountName ?? string.Empty;
                var confirmDialog = new ContentDialog
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = S.ConfirmStartServiceTitle,
                    Content = Strings.ConfirmStartServicePrompt(service.DisplayName, host),
                    PrimaryButtonText = S.ConfirmBtn,
                    CloseButtonText = S.CancelBtn,
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.StartServiceCommand.ExecuteAsync(service);
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

    private async void StopService_Click(object sender, RoutedEventArgs e)
    {
        if (_isDialogOpen) return;
        if (sender is Button btn && btn.Tag is ComputerServiceInfo service)
        {
            if (!service.CanStop) return;

            _isDialogOpen = true;
            try
            {
                string host = ViewModel.CurrentComputer?.SamAccountName ?? string.Empty;
                var confirmDialog = new ContentDialog
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = S.ConfirmStopServiceTitle,
                    Content = Strings.ConfirmStopServicePrompt(service.DisplayName, host),
                    PrimaryButtonText = S.ConfirmBtn,
                    CloseButtonText = S.CancelBtn,
                    DefaultButton = ContentDialogButton.Close // Default to Cancel to prevent accidental stoppage
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.StopServiceCommand.ExecuteAsync(service);
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

    private async void RestartService_Click(object sender, RoutedEventArgs e)
    {
        if (_isDialogOpen) return;
        if (sender is Button btn && btn.Tag is ComputerServiceInfo service)
        {
            if (!service.CanRestart) return;

            _isDialogOpen = true;
            try
            {
                string host = ViewModel.CurrentComputer?.SamAccountName ?? string.Empty;
                var confirmDialog = new ContentDialog
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = S.ConfirmRestartServiceTitle,
                    Content = Strings.ConfirmRestartServicePrompt(service.DisplayName, host),
                    PrimaryButtonText = S.ConfirmBtn,
                    CloseButtonText = S.CancelBtn,
                    DefaultButton = ContentDialogButton.Close // Default to Cancel
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.RestartServiceCommand.ExecuteAsync(service);
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

    private async void StartModeCombo_DropDownClosed(object sender, object e)
    {
        if (_isDialogOpen) return;
        if (sender is ComboBox combo && combo.Tag is ComputerServiceInfo service)
        {
            string newMode = combo.SelectedIndex switch
            {
                0 => "Auto",
                1 => "Manual",
                2 => "Disabled",
                _ => service.NormalizedStartMode
            };

            // Check if unchanged
            if (string.Equals(service.NormalizedStartMode, newMode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _isDialogOpen = true;
            try
            {
                string host = ViewModel.CurrentComputer?.SamAccountName ?? string.Empty;
                string localizedModeName = newMode switch
                {
                    "Auto" => S.ServiceStartModeAuto,
                    "Disabled" => S.ServiceStartModeDisabled,
                    _ => S.ServiceStartModeManual
                };

                var confirmDialog = new ContentDialog
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = S.ConfirmChangeStartupTypeTitle,
                    Content = Strings.ConfirmChangeStartupTypePrompt(service.DisplayName, host, localizedModeName),
                    PrimaryButtonText = S.ConfirmBtn,
                    CloseButtonText = S.CancelBtn,
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await ViewModel.ChangeServiceStartModeCommand.ExecuteAsync((service, newMode));
                }
                else
                {
                    // Revert combo selection back to original
                    combo.SelectedIndex = service.StartModeIndex;
                }
            }
            catch (Exception ex)
            {
                ViewModel.ShowError(ex.Message);
                combo.SelectedIndex = service.StartModeIndex;
            }
            finally
            {
                _isDialogOpen = false;
            }
        }
    }
}
