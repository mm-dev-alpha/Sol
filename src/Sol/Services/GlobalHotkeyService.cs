using System;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Helpers;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Implements global system hotkey management and Win32 WM_HOTKEY window subclass routing.
/// </summary>
public sealed class GlobalHotkeyService : IGlobalHotkeyService
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

    private readonly IMmcLookupService _mmcService;
    private readonly IAdminCommandService _adminCommandService;
    private readonly IShortcutGuideService _shortcutGuideService;
    private readonly IGrabFrameService _grabFrameService;
    private readonly IEditTextService _editTextService;
    private readonly ISettingsService _settingsService;

    private IntPtr _hwnd = IntPtr.Zero;
    private SubclassProc? _subclassProc;
    private bool _disposed;

    public event EventHandler? MmcLookupTriggered;
    public event EventHandler? AdminCommandsTriggered;
    public event EventHandler? ShortcutGuideTriggered;
    public event EventHandler? GrabFrameTriggered;
    public event EventHandler? EditTextTriggered;
    public event EventHandler<EditTextOpenEventArgs>? EditTextOpenRequested;

    public GlobalHotkeyService(
        IMmcLookupService mmcService,
        IAdminCommandService adminCommandService,
        IShortcutGuideService shortcutGuideService,
        IGrabFrameService grabFrameService,
        IEditTextService editTextService,
        ISettingsService settingsService)
    {
        _mmcService = mmcService ?? throw new ArgumentNullException(nameof(mmcService));
        _adminCommandService = adminCommandService ?? throw new ArgumentNullException(nameof(adminCommandService));
        _shortcutGuideService = shortcutGuideService ?? throw new ArgumentNullException(nameof(shortcutGuideService));
        _grabFrameService = grabFrameService ?? throw new ArgumentNullException(nameof(grabFrameService));
        _editTextService = editTextService ?? throw new ArgumentNullException(nameof(editTextService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        _mmcService.OpenRequested += OnMmcOpenRequested;
        _adminCommandService.OpenRequested += OnAdminCommandOpenRequested;
        _shortcutGuideService.OverlayToggleRequested += OnShortcutGuideToggleRequested;
        _editTextService.ToggleRequested += OnEditTextToggleRequested;
        _editTextService.OpenRequested += OnEditTextOpenRequested;

        WeakReferenceMessenger.Default.Register<ToolsSettingsChangedMessage>(this, (r, m) =>
        {
            UpdateHotkeys(m.IsMmcLookupEnabled, m.IsAdminCommandsEnabled, m.IsShortcutGuideEnabled, m.IsGrabFrameEnabled, m.IsEditTextWindowEnabled);
        });
    }

    private void OnMmcOpenRequested(object? sender, EventArgs e) => MmcLookupTriggered?.Invoke(this, EventArgs.Empty);
    private void OnAdminCommandOpenRequested(object? sender, EventArgs e) => AdminCommandsTriggered?.Invoke(this, EventArgs.Empty);
    private void OnShortcutGuideToggleRequested(object? sender, EventArgs e) => ShortcutGuideTriggered?.Invoke(this, EventArgs.Empty);
    private void OnEditTextToggleRequested(object? sender, EventArgs e) => EditTextTriggered?.Invoke(this, EventArgs.Empty);
    private void OnEditTextOpenRequested(object? sender, EditTextOpenEventArgs e) => EditTextOpenRequested?.Invoke(this, e);

    public void Initialize(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            AppLog.Write("GlobalHotkeyService.Initialize: hWnd is Zero!");
            return;
        }

        if (_hwnd != IntPtr.Zero && _hwnd != hWnd)
        {
            Detach();
        }

        _hwnd = hWnd;
        _subclassProc = new SubclassProc(WndProc);
        bool subclassResult = SetWindowSubclass(_hwnd, _subclassProc, (UIntPtr)SUBCLASS_ID, UIntPtr.Zero);
        AppLog.Write($"GlobalHotkeyService.Initialize: SetWindowSubclass result = {subclassResult}, lastErr = {Marshal.GetLastWin32Error()}");

        UpdateHotkeys(
            _settingsService.IsMmcLookupEnabled,
            _settingsService.IsAdminCommandsEnabled,
            _settingsService.IsShortcutGuideEnabled,
            _settingsService.IsGrabFrameEnabled,
            _settingsService.IsEditTextWindowEnabled);
    }

    public void UpdateHotkeys(bool mmcEnabled, bool adminCommandsEnabled, bool shortcutGuideEnabled, bool grabFrameEnabled = true, bool editTextEnabled = true)
    {
        if (_hwnd == IntPtr.Zero)
        {
            AppLog.Write("GlobalHotkeyService.UpdateHotkeys: _hwnd is Zero!");
            return;
        }

        try
        {
            if (mmcEnabled)
            {
                bool r = _mmcService.RegisterGlobalHotkey(_hwnd);
                AppLog.Write($"GlobalHotkeyService: MMC registered = {r}");
            }
            else
            {
                _mmcService.UnregisterGlobalHotkey(_hwnd);
            }

            if (adminCommandsEnabled)
            {
                bool r = _adminCommandService.RegisterGlobalHotkey(_hwnd);
                AppLog.Write($"GlobalHotkeyService: AdminCommands registered = {r}");
            }
            else
            {
                _adminCommandService.UnregisterGlobalHotkey(_hwnd);
            }

            if (shortcutGuideEnabled)
            {
                bool r = _shortcutGuideService.RegisterGlobalHotkey(_hwnd);
                AppLog.Write($"GlobalHotkeyService: ShortcutGuide registered = {r}");
            }
            else
            {
                _shortcutGuideService.UnregisterGlobalHotkey(_hwnd);
            }

            if (grabFrameEnabled)
            {
                bool r = _grabFrameService.RegisterGlobalHotkey(_hwnd);
                AppLog.Write($"GlobalHotkeyService: GrabFrame registered = {r}");
            }
            else
            {
                _grabFrameService.UnregisterGlobalHotkey(_hwnd);
            }

            if (editTextEnabled)
            {
                bool r = _editTextService.RegisterGlobalHotkey(_hwnd);
                AppLog.Write($"GlobalHotkeyService: EditText registered = {r}");
            }
            else
            {
                _editTextService.UnregisterGlobalHotkey(_hwnd);
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"GlobalHotkeyService.UpdateHotkeys exception: {ex}");
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData)
    {
        if (uMsg == WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            AppLog.Write($"GlobalHotkeyService.WndProc: WM_HOTKEY received! hotkeyId = 0x{hotkeyId:X4}");
            switch (hotkeyId)
            {
                case HOTKEY_MMC_LOOKUP_ID:
                    MmcLookupTriggered?.Invoke(this, EventArgs.Empty);
                    return IntPtr.Zero;
                case HOTKEY_ADMIN_COMMANDS_ID:
                    AdminCommandsTriggered?.Invoke(this, EventArgs.Empty);
                    return IntPtr.Zero;
                case HOTKEY_SHORTCUT_GUIDE_ID:
                    ShortcutGuideTriggered?.Invoke(this, EventArgs.Empty);
                    return IntPtr.Zero;
                case HOTKEY_GRAB_FRAME_ID:
                    GrabFrameTriggered?.Invoke(this, EventArgs.Empty);
                    return IntPtr.Zero;
                case HOTKEY_EDIT_TEXT_ID:
                    EditTextTriggered?.Invoke(this, EventArgs.Empty);
                    return IntPtr.Zero;
            }
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void Detach()
    {
        if (_hwnd != IntPtr.Zero)
        {
            try
            {
                if (_subclassProc != null)
                {
                    RemoveWindowSubclass(_hwnd, _subclassProc, (UIntPtr)SUBCLASS_ID);
                    _subclassProc = null;
                }

                _mmcService.UnregisterGlobalHotkey(_hwnd);
                _adminCommandService.UnregisterGlobalHotkey(_hwnd);
                _shortcutGuideService.UnregisterGlobalHotkey(_hwnd);
                _grabFrameService.UnregisterGlobalHotkey(_hwnd);
                _editTextService.UnregisterGlobalHotkey(_hwnd);
            }
            catch { }
            finally
            {
                _hwnd = IntPtr.Zero;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _mmcService.OpenRequested -= OnMmcOpenRequested;
        _adminCommandService.OpenRequested -= OnAdminCommandOpenRequested;
        _shortcutGuideService.OverlayToggleRequested -= OnShortcutGuideToggleRequested;
        _editTextService.ToggleRequested -= OnEditTextToggleRequested;
        _editTextService.OpenRequested -= OnEditTextOpenRequested;

        WeakReferenceMessenger.Default.UnregisterAll(this);
        Detach();
        GC.SuppressFinalize(this);
    }
}
