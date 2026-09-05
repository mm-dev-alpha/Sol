using System;
using System.Collections.Generic;
using Sol.Models;

namespace Sol.Services;

public interface IAdminCommandService : IDisposable
{
    event EventHandler? FavoritesChanged;
    event EventHandler? OpenRequested;

    IReadOnlyList<AdminCommandItem> GetAllCommands();
    IReadOnlyList<AdminCommandItem> SearchCommands(string? query = null, AdminCommandCategory? category = null, AdminShellType? shellType = null);
    void ToggleFavorite(string id);
    bool IsFavorite(string id);
    string CopyCommand(AdminCommandItem item);
    void RequestOpen();

    bool RegisterGlobalHotkey(IntPtr hWnd);
    void UnregisterGlobalHotkey(IntPtr hWnd);
}
