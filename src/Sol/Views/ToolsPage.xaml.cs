using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sol.Helpers;
using Sol.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Sol.Views;

public sealed partial class ToolsPage : Page
{
    public ToolsViewModel ViewModel { get; }
    public Strings S => Strings.S;

    public ToolsPage()
    {
        ViewModel = App.GetService<ToolsViewModel>();
        InitializeComponent();
    }

    private void LocksmithDropZone_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = S.FileLocksmithTitle;
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
    }

    private async void LocksmithDropZone_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0)
            {
                var path = items[0].Path;
                ViewModel.OpenFileLocksmithWindow(path);
            }
        }
    }

    private async void LocksmithBrowseFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ViewModel.OpenFileLocksmithWindow(file.Path);
            }
        }
        catch { }
    }
}
