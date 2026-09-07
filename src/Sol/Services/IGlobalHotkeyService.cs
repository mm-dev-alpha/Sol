using System;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service responsible for managing global system hotkeys and routing Win32 WM_HOTKEY activations.
/// </summary>
public interface IGlobalHotkeyService : IDisposable
{
    /// <summary>
    /// Attaches the hotkey message hook to the specified window handle and registers enabled hotkeys.
    /// </summary>
    void Initialize(IntPtr hWnd);

    /// <summary>
    /// Updates registration state of global hotkeys based on user configuration settings.
    /// </summary>
    void UpdateHotkeys(bool mmcEnabled, bool adminCommandsEnabled, bool shortcutGuideEnabled, bool grabFrameEnabled, bool editTextEnabled);

    /// <summary>
    /// Triggered when the MMC Lookup hotkey or open request is activated.
    /// </summary>
    event EventHandler? MmcLookupTriggered;

    /// <summary>
    /// Triggered when the Admin Commands hotkey or open request is activated.
    /// </summary>
    event EventHandler? AdminCommandsTriggered;

    /// <summary>
    /// Triggered when the Shortcut Guide hotkey or toggle request is activated.
    /// </summary>
    event EventHandler? ShortcutGuideTriggered;

    /// <summary>
    /// Triggered when the Grab Frame hotkey is activated.
    /// </summary>
    event EventHandler? GrabFrameTriggered;

    /// <summary>
    /// Triggered when the Edit Text hotkey or toggle request is activated.
    /// </summary>
    event EventHandler? EditTextTriggered;

    /// <summary>
    /// Triggered when Edit Text is explicitly requested with initial text or table payload.
    /// </summary>
    event EventHandler<EditTextOpenEventArgs>? EditTextOpenRequested;
}
