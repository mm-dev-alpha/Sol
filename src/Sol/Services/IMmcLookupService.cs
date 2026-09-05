using System;
using System.Collections.Generic;
using Sol.Models;

namespace Sol.Services;

public interface IMmcLookupService : IDisposable
{
    event EventHandler? FavoritesChanged;
    event EventHandler? OpenRequested;

    IReadOnlyList<MmcToolItem> GetAllTools();
    IReadOnlyList<MmcToolItem> SearchTools(string? query = null, MmcCategory? category = null);
    void ToggleFavorite(string id);
    bool IsFavorite(string id);
    bool LaunchTool(MmcToolItem tool, out string? errorMessage);
    string CopyRunCommand(MmcToolItem tool);
    void RequestOpen();

    bool RegisterGlobalHotkey(IntPtr hWnd);
    void UnregisterGlobalHotkey(IntPtr hWnd);
}
