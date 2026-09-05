using System;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

public interface IEditTextService
{
    bool RegisterGlobalHotkey(IntPtr hWnd);
    void UnregisterGlobalHotkey(IntPtr hWnd);
    CalculationResult EvaluateExpressions(string input);
    Task<bool> TryInsertTextAsync(string text);
    event EventHandler? ToggleRequested;
    event EventHandler<EditTextOpenEventArgs>? OpenRequested;
    void RequestToggle();
    void RequestOpen(string? text = null, EditTextTableDocument? table = null);
}
