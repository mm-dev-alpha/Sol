using System;
using System.Collections.Generic;
using Sol.Models;

namespace Sol.Services;

public interface IShortcutGuideService : IDisposable
{
    IReadOnlyList<ShortcutCategory> GetCategories();
    IReadOnlyList<ShortcutItem> GetAllShortcuts();
    IReadOnlyList<ShortcutItem> SearchShortcuts(string query);
    bool RegisterGlobalHotkey(IntPtr hWnd);
    bool UnregisterGlobalHotkey(IntPtr hWnd);
    void ToggleOverlay();
    event EventHandler? OverlayToggleRequested;
}
