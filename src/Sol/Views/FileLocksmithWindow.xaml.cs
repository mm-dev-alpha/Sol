using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Sol.Helpers;
using Sol.Models;
using Sol.ViewModels;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Sol.Views;

public sealed partial class FileLocksmithWindow : Window
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    public static FileLocksmithWindow? CurrentActiveInstance { get; private set; }

    public FileLocksmithViewModel ViewModel { get; }
    public Strings S => Strings.S;

    public FileLocksmithWindow(FileLocksmithViewModel viewModel, string? initialPath = null)
    {
        CurrentActiveInstance = this;
        Closed += (s, e) =>
        {
            if (CurrentActiveInstance == this)
            {
                CurrentActiveInstance = null;
            }
        };

        ViewModel = viewModel;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        Title = S.FileLocksmithTitle;
        TitleTextBlock.Text = S.FileLocksmithTitle;

        CenterAndResizeWindow();

        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            InspectPath(initialPath);
        }
    }

    public void InspectPath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            ViewModel.TargetPath = path;
            _ = ViewModel.InspectLocksAsync(path);
        }
    }

    public void BringToForeground()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
        }
        catch { }
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
                int width = 940;
                int height = 620;
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

    private void InspectButton_Click(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.InspectLocksAsync();
    }

    private void PathTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            _ = ViewModel.InspectLocksAsync();
        }
    }

    private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ViewModel.TargetPath = file.Path;
                await ViewModel.InspectLocksAsync(file.Path);
            }
        }
        catch { }
    }

    private async void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                ViewModel.TargetPath = folder.Path;
                await ViewModel.InspectLocksAsync(folder.Path);
            }
        }
        catch { }
    }

    private void EndTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LockingProcessInfo process })
        {
            ViewModel.EndTask(process);
        }
    }

    private async void EndAllButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = S.FileLocksmithConfirmKillAllTitle,
            Content = S.FileLocksmithConfirmKillAllMessage,
            PrimaryButtonText = S.FileLocksmithEndAllBtn,
            CloseButtonText = S.CancelBtn,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.EndAllTasks();
        }
    }
}
