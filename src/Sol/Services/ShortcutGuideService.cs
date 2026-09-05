using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Sol.Helpers;
using Sol.Models;

namespace Sol.Services;

public class ShortcutGuideService : IShortcutGuideService
{
    private const int HOTKEY_ID = 0x5347; // 'SG'
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_OEM_2 = 0xBF; // '?' or '/'
    private const uint VK_OEM_4 = 0xDB; // '?' (Shift + ß) on German keyboard

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly List<ShortcutCategory> _categories;
    private IntPtr _registeredHwnd = IntPtr.Zero;

    public event EventHandler? OverlayToggleRequested;

    public ShortcutGuideService()
    {
        var s = Strings.S;

        _categories =
        [
            new(s.ShortcutCatSystem,
            [
                new("Win", ["Win"], s.ShortcutDescStartMenu, s.ShortcutCatSystem),
                new("Win + A", ["Win", "A"], s.ShortcutDescQuickSettings, s.ShortcutCatSystem),
                new("Win + N", ["Win", "N"], s.ShortcutDescNotificationCenter, s.ShortcutCatSystem),
                new("Win + E", ["Win", "E"], s.ShortcutDescFileExplorer, s.ShortcutCatSystem),
                new("Win + I", ["Win", "I"], s.ShortcutDescWindowsSettings, s.ShortcutCatSystem),
                new("Win + L", ["Win", "L"], s.ShortcutDescLockPc, s.ShortcutCatSystem),
                new("Win + D", ["Win", "D"], s.ShortcutDescDesktop, s.ShortcutCatSystem),
                new("Win + V", ["Win", "V"], s.ShortcutDescClipboardHistory, s.ShortcutCatSystem),
                new("Win + X", ["Win", "X"], s.ShortcutDescQuickLink, s.ShortcutCatSystem),
                new("Win + Tab", ["Win", "Tab"], s.ShortcutDescTaskView, s.ShortcutCatSystem)
            ]),

            new(s.ShortcutCatWindowManagement,
            [
                new("Win + Z", ["Win", "Z"], s.ShortcutDescSnapLayouts, s.ShortcutCatWindowManagement),
                new("Win + Left", ["Win", "←"], s.ShortcutDescSnapLeft, s.ShortcutCatWindowManagement),
                new("Win + Right", ["Win", "→"], s.ShortcutDescSnapRight, s.ShortcutCatWindowManagement),
                new("Win + Up", ["Win", "↑"], s.ShortcutDescMaximize, s.ShortcutCatWindowManagement),
                new("Win + Down", ["Win", "↓"], s.ShortcutDescMinimize, s.ShortcutCatWindowManagement),
                new("Win + Shift + Left", ["Win", "Shift", "←"], s.ShortcutDescMoveLeftMonitor, s.ShortcutCatWindowManagement),
                new("Win + Shift + Right", ["Win", "Shift", "→"], s.ShortcutDescMoveRightMonitor, s.ShortcutCatWindowManagement)
            ]),

            new(s.ShortcutCatVirtualDesktops,
            [
                new("Win + Ctrl + D", ["Win", "Ctrl", "D"], s.ShortcutDescNewVirtualDesktop, s.ShortcutCatVirtualDesktops),
                new("Win + Ctrl + Left", ["Win", "Ctrl", "←"], s.ShortcutDescSwitchVirtualDesktopLeft, s.ShortcutCatVirtualDesktops),
                new("Win + Ctrl + Right", ["Win", "Ctrl", "→"], s.ShortcutDescSwitchVirtualDesktopRight, s.ShortcutCatVirtualDesktops),
                new("Win + Ctrl + F4", ["Win", "Ctrl", "F4"], s.ShortcutDescCloseVirtualDesktop, s.ShortcutCatVirtualDesktops),
                new("Win + P", ["Win", "P"], s.ShortcutDescProject, s.ShortcutCatVirtualDesktops),
                new("Win + K", ["Win", "K"], s.ShortcutDescCast, s.ShortcutCatVirtualDesktops)
            ]),

            new(s.ShortcutCatToolsAndCapture,
            [
                new("Win + Shift + S", ["Win", "Shift", "S"], s.ShortcutDescScreenshot, s.ShortcutCatToolsAndCapture),
                new("Win + G", ["Win", "G"], s.ShortcutDescXboxGameBar, s.ShortcutCatToolsAndCapture),
                new("Win + H", ["Win", "H"], s.ShortcutDescVoiceTyping, s.ShortcutCatToolsAndCapture),
                new("Win + W", ["Win", "W"], s.ShortcutDescWidgets, s.ShortcutCatToolsAndCapture),
                new("Win + .", ["Win", "."], s.ShortcutDescEmoji, s.ShortcutCatToolsAndCapture)
            ]),

            new(s.ShortcutCatTaskbar,
            [
                new("Win + 1..9", ["Win", "1..9"], s.ShortcutDescTaskbarNumbered, s.ShortcutCatTaskbar),
                new("Win + Shift + 1..9", ["Win", "Shift", "1..9"], s.ShortcutDescTaskbarNumberedNewInstance, s.ShortcutCatTaskbar)
            ]),

            new(s.ShortcutCatGeneralEditing,
            [
                new("Ctrl + C", ["Ctrl", "C"], s.ShortcutDescCopy, s.ShortcutCatGeneralEditing),
                new("Ctrl + V", ["Ctrl", "V"], s.ShortcutDescPaste, s.ShortcutCatGeneralEditing),
                new("Ctrl + X", ["Ctrl", "X"], s.ShortcutDescCut, s.ShortcutCatGeneralEditing),
                new("Ctrl + Z", ["Ctrl", "Z"], s.ShortcutDescUndo, s.ShortcutCatGeneralEditing),
                new("Ctrl + Y", ["Ctrl", "Y"], s.ShortcutDescRedo, s.ShortcutCatGeneralEditing),
                new("Ctrl + A", ["Ctrl", "A"], s.ShortcutDescSelectAll, s.ShortcutCatGeneralEditing),
                new("Ctrl + F", ["Ctrl", "F"], s.ShortcutDescFind, s.ShortcutCatGeneralEditing),
                new("Ctrl + S", ["Ctrl", "S"], s.ShortcutDescSave, s.ShortcutCatGeneralEditing),
                new("Ctrl + Shift + Esc", ["Ctrl", "Shift", "Esc"], s.ShortcutDescTaskManager, s.ShortcutCatGeneralEditing),
                new("Alt + Tab", ["Alt", "Tab"], s.ShortcutDescSwitchApps, s.ShortcutCatGeneralEditing),
                new("Alt + F4", ["Alt", "F4"], s.ShortcutDescCloseApp, s.ShortcutCatGeneralEditing),
                new("Alt + Esc", ["Alt", "Esc"], s.ShortcutDescCycleApps, s.ShortcutCatGeneralEditing)
            ]),

            new(s.ShortcutCatFileExplorerNav,
            [
                new("F2", ["F2"], s.ShortcutDescRename, s.ShortcutCatFileExplorerNav),
                new("F5", ["F5"], s.ShortcutDescRefresh, s.ShortcutCatFileExplorerNav),
                new("Ctrl + Shift + N", ["Ctrl", "Shift", "N"], s.ShortcutDescNewFolder, s.ShortcutCatFileExplorerNav),
                new("Alt + Enter", ["Alt", "Enter"], s.ShortcutDescProperties, s.ShortcutCatFileExplorerNav),
                new("Alt + Left", ["Alt", "←"], s.ShortcutDescBack, s.ShortcutCatFileExplorerNav),
                new("Alt + Right", ["Alt", "→"], s.ShortcutDescForward, s.ShortcutCatFileExplorerNav),
                new("Alt + Up", ["Alt", "↑"], s.ShortcutDescUpFolder, s.ShortcutCatFileExplorerNav),
                new("Ctrl + W", ["Ctrl", "W"], s.ShortcutDescCloseTab, s.ShortcutCatFileExplorerNav),
                new("Shift + Delete", ["Shift", "Delete"], s.ShortcutDescDeletePermanent, s.ShortcutCatFileExplorerNav)
            ]),

            new(s.ShortcutCatAppNavigation,
            [
                new("Ctrl + T", ["Ctrl", "T"], s.ShortcutDescNewTab, s.ShortcutCatAppNavigation),
                new("Ctrl + N", ["Ctrl", "N"], s.ShortcutDescNewWindow, s.ShortcutCatAppNavigation),
                new("Ctrl + Shift + T", ["Ctrl", "Shift", "T"], s.ShortcutDescReopenTab, s.ShortcutCatAppNavigation),
                new("Ctrl + L", ["Ctrl", "L"], s.ShortcutDescAddressBar, s.ShortcutCatAppNavigation),
                new("Ctrl + Tab", ["Ctrl", "Tab"], s.ShortcutDescNextTab, s.ShortcutCatAppNavigation),
                new("Ctrl + Shift + Tab", ["Ctrl", "Shift", "Tab"], s.ShortcutDescPrevTab, s.ShortcutCatAppNavigation)
            ])
        ];
    }

    public IReadOnlyList<ShortcutCategory> GetCategories() => _categories;

    public IReadOnlyList<ShortcutItem> GetAllShortcuts() => _categories.SelectMany(c => c.Shortcuts).ToList();

    public IReadOnlyList<ShortcutItem> SearchShortcuts(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAllShortcuts();

        var q = query.Trim();
        return GetAllShortcuts().Where(s =>
            s.PrimaryKeyCombination.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            s.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            s.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            s.Keys.Any(k => k.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    public bool RegisterGlobalHotkey(IntPtr hWnd)
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        _registeredHwnd = hWnd;
        bool registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT | MOD_NOREPEAT, VK_OEM_2);
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_OEM_2);
        }
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT | MOD_NOREPEAT, VK_OEM_4);
        }
        return registered;
    }

    public bool UnregisterGlobalHotkey(IntPtr hWnd)
    {
        if (_registeredHwnd != IntPtr.Zero)
        {
            var res = UnregisterHotKey(_registeredHwnd, HOTKEY_ID);
            _registeredHwnd = IntPtr.Zero;
            return res;
        }
        return true;
    }

    public void ToggleOverlay()
    {
        OverlayToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        UnregisterGlobalHotkey(_registeredHwnd);
    }
}
