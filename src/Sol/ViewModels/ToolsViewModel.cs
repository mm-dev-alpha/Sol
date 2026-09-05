using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;

namespace Sol.ViewModels;

public partial class ToolsViewModel : ObservableObject
{
    public Strings S => Strings.S;

    [ObservableProperty]
    public partial bool IsAwakeActive { get; set; }

    [ObservableProperty]
    public partial string AwakeStatusBadge { get; set; }

    [ObservableProperty]
    public partial int AwakeModeIndex { get; set; } = 0; // 0: Indefinite, 1: 30m, 2: 1h, 3: 2h

    [ObservableProperty]
    public partial bool KeepScreenOn { get; set; } = false;

    [ObservableProperty]
    public partial string AwakeCountdownText { get; set; } = string.Empty;

    public ObservableCollection<string> AwakeModes { get; } = [];

    [ObservableProperty]
    public partial string FileLocksmithStatusBadge { get; set; }

    [ObservableProperty]
    public partial string ShortcutGuideStatusBadge { get; set; }

    [ObservableProperty]
    public partial string ShortcutGuideHotkeyText { get; set; } = "Win + Shift + ?";

    [ObservableProperty]
    public partial string MmcStatusBadge { get; set; }

    [ObservableProperty]
    public partial string MmcHotkeyText { get; set; } = "Alt + Space";

    [ObservableProperty]
    public partial string AdminCommandsStatusBadge { get; set; }

    [ObservableProperty]
    public partial string AdminCommandsHotkeyText { get; set; } = "Win + Shift + C";

    [ObservableProperty]
    public partial string GrabFrameStatusBadge { get; set; }

    [ObservableProperty]
    public partial string GrabFrameHotkeyText { get; set; } = "Win + Shift + G";

    [ObservableProperty]
    public partial string EditTextStatusBadge { get; set; }

    [ObservableProperty]
    public partial string EditTextHotkeyText { get; set; } = "Win + Shift + E";

    private readonly IAwakeService? _awakeService;
    private readonly IShortcutGuideService? _shortcutGuideService;
    private readonly IMmcLookupService? _mmcLookupService;
    private readonly IAdminCommandService? _adminCommandService;
    private readonly INavigationService? _navigationService;
    private readonly ISettingsService? _settingsService;
    private bool _isUpdatingState;

    public ToolsViewModel(
        IAwakeService? awakeService = null, 
        IShortcutGuideService? shortcutGuideService = null,
        IMmcLookupService? mmcLookupService = null,
        IAdminCommandService? adminCommandService = null,
        INavigationService? navigationService = null,
        ISettingsService? settingsService = null)
    {
        _awakeService = awakeService;
        _shortcutGuideService = shortcutGuideService;
        _mmcLookupService = mmcLookupService;
        _adminCommandService = adminCommandService;
        _navigationService = navigationService;
        _settingsService = settingsService;

        AwakeStatusBadge = S.ToolStatusInactive;
        FileLocksmithStatusBadge = S.ToolStatusReady;
        ShortcutGuideStatusBadge = (_settingsService?.IsShortcutGuideEnabled ?? true) ? S.ToolStatusReady : S.ToolStatusInactive;
        MmcStatusBadge = (_settingsService?.IsMmcLookupEnabled ?? true) ? S.ToolStatusReady : S.ToolStatusInactive;
        AdminCommandsStatusBadge = (_settingsService?.IsAdminCommandsEnabled ?? true) ? S.ToolStatusReady : S.ToolStatusInactive;
        GrabFrameStatusBadge = (_settingsService?.IsGrabFrameEnabled ?? true) ? S.ToolStatusReady : S.ToolStatusInactive;
        EditTextStatusBadge = (_settingsService?.IsEditTextWindowEnabled ?? true) ? S.ToolStatusReady : S.ToolStatusInactive;

        if (_settingsService != null)
        {
            KeepScreenOn = _settingsService.AwakeKeepDisplayOnDefault;
        }

        if (_shortcutGuideService != null)
        {
            _shortcutGuideService.OverlayToggleRequested += (s, e) => OpenShortcutGuide();
        }

        if (_mmcLookupService != null)
        {
            _mmcLookupService.OpenRequested += (s, e) => LaunchMmcLookup();
        }

        if (_adminCommandService != null)
        {
            _adminCommandService.OpenRequested += (s, e) => LaunchAdminCommands();
        }

        WeakReferenceMessenger.Default.Register<ToolsSettingsChangedMessage>(this, (r, m) =>
        {
            ShortcutGuideStatusBadge = m.IsShortcutGuideEnabled ? S.ToolStatusReady : S.ToolStatusInactive;
            MmcStatusBadge = m.IsMmcLookupEnabled ? S.ToolStatusReady : S.ToolStatusInactive;
            AdminCommandsStatusBadge = m.IsAdminCommandsEnabled ? S.ToolStatusReady : S.ToolStatusInactive;
            GrabFrameStatusBadge = m.IsGrabFrameEnabled ? S.ToolStatusReady : S.ToolStatusInactive;
            EditTextStatusBadge = m.IsEditTextWindowEnabled ? S.ToolStatusReady : S.ToolStatusInactive;
        });

        AwakeModes.Add(S.ToolAwakeModeIndefinite);
        AwakeModes.Add(S.ToolAwakeMode30m);
        AwakeModes.Add(S.ToolAwakeMode1h);
        AwakeModes.Add(S.ToolAwakeMode2h);

        if (_awakeService != null)
        {
            _awakeService.StateChanged += OnAwakeServiceStateChanged;
            _awakeService.TimeRemainingTick += OnAwakeServiceTimeRemainingTick;
            _awakeService.TimedSessionExpired += OnAwakeServiceTimedSessionExpired;

            SyncWithAwakeService();
        }
    }

    private void SyncWithAwakeService()
    {
        if (_awakeService == null) return;

        _isUpdatingState = true;
        try
        {
            IsAwakeActive = _awakeService.IsActive;
            AwakeStatusBadge = _awakeService.IsActive ? S.ToolStatusActive : S.ToolStatusInactive;
            KeepScreenOn = _awakeService.KeepDisplayOn;

            if (_awakeService.Mode == AwakeMode.Indefinite)
            {
                AwakeModeIndex = 0;
                AwakeCountdownText = string.Empty;
            }
            else if (_awakeService.Mode == AwakeMode.Timed && _awakeService.RemainingTime.HasValue)
            {
                var rem = _awakeService.RemainingTime.Value;
                AwakeCountdownText = $"{S.ToolAwakeRemainingTime}{rem:hh\\:mm\\:ss}";
            }
            else
            {
                AwakeCountdownText = string.Empty;
            }
        }
        finally
        {
            _isUpdatingState = false;
        }
    }

    partial void OnIsAwakeActiveChanged(bool value)
    {
        if (_isUpdatingState || _awakeService == null) return;
        ApplyAwakeState();
    }

    partial void OnAwakeModeIndexChanged(int value)
    {
        if (_isUpdatingState || _awakeService == null || !IsAwakeActive) return;
        ApplyAwakeState();
    }

    partial void OnKeepScreenOnChanged(bool value)
    {
        if (_isUpdatingState || _awakeService == null) return;
        _awakeService.KeepDisplayOn = value;
        if (IsAwakeActive)
        {
            ApplyAwakeState();
        }
    }

    private void ApplyAwakeState()
    {
        if (_awakeService == null) return;

        if (!IsAwakeActive)
        {
            _awakeService.SetPassive();
            AwakeStatusBadge = S.ToolStatusInactive;
            AwakeCountdownText = string.Empty;
            return;
        }

        AwakeStatusBadge = S.ToolStatusActive;

        switch (AwakeModeIndex)
        {
            case 0:
                _awakeService.SetIndefinite(KeepScreenOn);
                AwakeCountdownText = string.Empty;
                break;
            case 1:
                _awakeService.SetTimed(TimeSpan.FromMinutes(30), KeepScreenOn);
                break;
            case 2:
                _awakeService.SetTimed(TimeSpan.FromHours(1), KeepScreenOn);
                break;
            case 3:
                _awakeService.SetTimed(TimeSpan.FromHours(2), KeepScreenOn);
                break;
            default:
                _awakeService.SetIndefinite(KeepScreenOn);
                AwakeCountdownText = string.Empty;
                break;
        }
    }

    private void OnAwakeServiceStateChanged(object? sender, AwakeStateChangedEventArgs e)
    {
        _isUpdatingState = true;
        try
        {
            IsAwakeActive = e.IsActive;
            AwakeStatusBadge = e.IsActive ? S.ToolStatusActive : S.ToolStatusInactive;
            KeepScreenOn = e.KeepDisplayOn;

            if (e.Mode != AwakeMode.Timed)
            {
                AwakeCountdownText = string.Empty;
            }
        }
        finally
        {
            _isUpdatingState = false;
        }
    }

    private void OnAwakeServiceTimeRemainingTick(object? sender, TimeSpan remaining)
    {
        AwakeCountdownText = $"{S.ToolAwakeRemainingTime}{remaining:hh\\:mm\\:ss}";
    }

    private void OnAwakeServiceTimedSessionExpired(object? sender, EventArgs e)
    {
        _isUpdatingState = true;
        try
        {
            IsAwakeActive = false;
            AwakeStatusBadge = S.ToolStatusInactive;
            AwakeCountdownText = string.Empty;
        }
        finally
        {
            _isUpdatingState = false;
        }

        WeakReferenceMessenger.Default.Send(new AppNotificationMessage(
            S.ToolAwakeExpiredNotification,
            InfoBarSeverity.Informational));
    }

    [RelayCommand]
    public void OpenFileLocksmithWindow(string? path = null)
    {
        var vm = App.GetService<FileLocksmithViewModel>();
        var win = new Sol.Views.FileLocksmithWindow(vm, path);
        win.Activate();
    }

    [RelayCommand]
    public void OpenShortcutGuide()
    {
        try
        {
            AppLog.Write("ToolsViewModel.OpenShortcutGuide called");
            if (Sol.Views.ShortcutGuideOverlayWindow.CurrentInstance != null)
            {
                AppLog.Write("ToolsViewModel.OpenShortcutGuide: closing existing CurrentInstance");
                Sol.Views.ShortcutGuideOverlayWindow.CurrentInstance.Close();
                return;
            }

            var service = _shortcutGuideService ?? App.GetService<IShortcutGuideService>();
            var win = new Sol.Views.ShortcutGuideOverlayWindow(service);
            AppLog.Write("ToolsViewModel.OpenShortcutGuide: activating window");
            win.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"ToolsViewModel.OpenShortcutGuide exception: {ex}");
        }
    }

    [RelayCommand]
    public void LaunchMmcLookup()
    {
        try
        {
            AppLog.Write("ToolsViewModel.LaunchMmcLookup called");
            if (Sol.Views.MmcLookupWindow.CurrentInstance != null)
            {
                Sol.Views.MmcLookupWindow.CurrentInstance.Activate();
                return;
            }

            var service = _mmcLookupService ?? App.GetService<IMmcLookupService>();
            var win = new Sol.Views.MmcLookupWindow(service);
            AppLog.Write("ToolsViewModel.LaunchMmcLookup: activating window");
            win.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"ToolsViewModel.LaunchMmcLookup exception: {ex}");
        }
    }

    [RelayCommand]
    public void LaunchAdminCommands()
    {
        try
        {
            AppLog.Write("ToolsViewModel.LaunchAdminCommands called");
            if (Sol.Views.AdminCommandsWindow.CurrentInstance != null)
            {
                Sol.Views.AdminCommandsWindow.CurrentInstance.Activate();
                return;
            }

            var service = _adminCommandService ?? App.GetService<IAdminCommandService>();
            var win = new Sol.Views.AdminCommandsWindow(service);
            AppLog.Write("ToolsViewModel.LaunchAdminCommands: activating window");
            win.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"ToolsViewModel.LaunchAdminCommands exception: {ex}");
        }
    }

    [RelayCommand]
    public void LaunchGrabFrame()
    {
        try
        {
            AppLog.Write("ToolsViewModel.LaunchGrabFrame called");
            if (Sol.Views.GrabFrameWindow.CurrentInstance != null)
            {
                AppLog.Write("ToolsViewModel.LaunchGrabFrame: activating existing CurrentInstance");
                Sol.Views.GrabFrameWindow.CurrentInstance.Activate();
                return;
            }

            var captureService = App.GetService<IScreenCaptureService>();
            var ocrService = App.GetService<IOcrService>();
            var grabFrameService = App.GetService<IGrabFrameService>();
            var editTextService = App.GetService<IEditTextService>();
            var settingsService = _settingsService ?? App.GetService<ISettingsService>();
            var win = new Sol.Views.GrabFrameWindow(captureService, ocrService, grabFrameService, editTextService, settingsService);
            AppLog.Write("ToolsViewModel.LaunchGrabFrame: activating window");
            win.Activate();
        }
        catch (Exception ex)
        {
            AppLog.Write($"ToolsViewModel.LaunchGrabFrame exception: {ex}");
        }
    }

    [RelayCommand]
    public void LaunchEditTextWindow()
    {
        try
        {
            AppLog.Write("ToolsViewModel.LaunchEditTextWindow called");
            App.GetService<IEditTextService>()?.RequestOpen();
        }
        catch (Exception ex)
        {
            AppLog.Write($"ToolsViewModel.LaunchEditTextWindow exception: {ex}");
        }
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService?.NavigateTo("SettingsPage");
    }
}
