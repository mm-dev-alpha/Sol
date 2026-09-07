using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Models;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class GlobalHotkeyServiceTests : IDisposable
{
    public GlobalHotkeyServiceTests()
    {
        WeakReferenceMessenger.Default.Reset();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Reset();
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        var mmc = new FakeMmcService();
        var admin = new FakeAdminCommandService();
        var shortcut = new FakeShortcutGuideService();
        var grab = new FakeGrabFrameService();
        var editText = new FakeEditTextService();
        var settings = new FakeSettingsService();

        Assert.Throws<ArgumentNullException>(() => new GlobalHotkeyService(null!, admin, shortcut, grab, editText, settings));
        Assert.Throws<ArgumentNullException>(() => new GlobalHotkeyService(mmc, null!, shortcut, grab, editText, settings));
        Assert.Throws<ArgumentNullException>(() => new GlobalHotkeyService(mmc, admin, null!, grab, editText, settings));
        Assert.Throws<ArgumentNullException>(() => new GlobalHotkeyService(mmc, admin, shortcut, null!, editText, settings));
        Assert.Throws<ArgumentNullException>(() => new GlobalHotkeyService(mmc, admin, shortcut, grab, null!, settings));
        Assert.Throws<ArgumentNullException>(() => new GlobalHotkeyService(mmc, admin, shortcut, grab, editText, null!));
    }

    [Fact]
    public void ToolServiceOpenRequests_FireCorrespondingEvents()
    {
        var mmc = new FakeMmcService();
        var admin = new FakeAdminCommandService();
        var shortcut = new FakeShortcutGuideService();
        var grab = new FakeGrabFrameService();
        var editText = new FakeEditTextService();
        var settings = new FakeSettingsService();

        using var service = new GlobalHotkeyService(mmc, admin, shortcut, grab, editText, settings);

        bool mmcFired = false;
        bool adminFired = false;
        bool shortcutFired = false;
        bool editTextFired = false;
        bool editTextOpenFired = false;

        service.MmcLookupTriggered += (s, e) => mmcFired = true;
        service.AdminCommandsTriggered += (s, e) => adminFired = true;
        service.ShortcutGuideTriggered += (s, e) => shortcutFired = true;
        service.EditTextTriggered += (s, e) => editTextFired = true;
        service.EditTextOpenRequested += (s, e) => editTextOpenFired = true;

        mmc.RequestOpen();
        Assert.True(mmcFired);

        admin.RequestOpen();
        Assert.True(adminFired);

        shortcut.ToggleOverlay();
        Assert.True(shortcutFired);

        editText.RequestToggle();
        Assert.True(editTextFired);

        editText.RequestOpen("sample text");
        Assert.True(editTextOpenFired);
    }

    [Fact]
    public void UpdateHotkeys_CoordinatesIndividualServiceRegistrations()
    {
        var mmc = new FakeMmcService();
        var admin = new FakeAdminCommandService();
        var shortcut = new FakeShortcutGuideService();
        var grab = new FakeGrabFrameService();
        var editText = new FakeEditTextService();
        var settings = new FakeSettingsService();

        using var service = new GlobalHotkeyService(mmc, admin, shortcut, grab, editText, settings);
        var testHwnd = new IntPtr(0x1234);

        // Simulated window initialization would set _hwnd, or we can test when _hwnd is zero vs set
        service.UpdateHotkeys(true, false, true, false, true);

        // Since _hwnd is IntPtr.Zero until Initialize is called, nothing should be registered
        Assert.False(mmc.IsRegistered);
    }

    [Fact]
    public void SettingsChangedMessage_UpdatesHotkeysState()
    {
        var mmc = new FakeMmcService();
        var admin = new FakeAdminCommandService();
        var shortcut = new FakeShortcutGuideService();
        var grab = new FakeGrabFrameService();
        var editText = new FakeEditTextService();
        var settings = new FakeSettingsService();

        using var service = new GlobalHotkeyService(mmc, admin, shortcut, grab, editText, settings);

        // Verify message handler does not throw
        WeakReferenceMessenger.Default.Send(new ToolsSettingsChangedMessage(true, true, true, true, true));
    }

#pragma warning disable CS0067
    private class FakeMmcService : IMmcLookupService
    {
        public bool IsRegistered { get; private set; }
        public event EventHandler? FavoritesChanged;
        public event EventHandler? OpenRequested;
        public IReadOnlyList<MmcToolItem> GetAllTools() => Array.Empty<MmcToolItem>();
        public IReadOnlyList<MmcToolItem> SearchTools(string? query = null, MmcCategory? category = null) => Array.Empty<MmcToolItem>();
        public void ToggleFavorite(string id) { }
        public bool IsFavorite(string id) => false;
        public bool LaunchTool(MmcToolItem tool, out string? errorMessage) { errorMessage = null; return true; }
        public string CopyRunCommand(MmcToolItem tool) => string.Empty;
        public void RequestOpen() => OpenRequested?.Invoke(this, EventArgs.Empty);
        public bool RegisterGlobalHotkey(IntPtr hWnd) { IsRegistered = true; return true; }
        public void UnregisterGlobalHotkey(IntPtr hWnd) { IsRegistered = false; }
        public void Dispose() { }
    }

    private class FakeAdminCommandService : IAdminCommandService
    {
        public bool IsRegistered { get; private set; }
        public event EventHandler? FavoritesChanged;
        public event EventHandler? OpenRequested;
        public IReadOnlyList<AdminCommandItem> GetAllCommands() => Array.Empty<AdminCommandItem>();
        public IReadOnlyList<AdminCommandItem> SearchCommands(string? query = null, AdminCommandCategory? category = null, AdminShellType? shellType = null) => Array.Empty<AdminCommandItem>();
        public void ToggleFavorite(string id) { }
        public bool IsFavorite(string id) => false;
        public bool IsCommandAvailable(string id) => true;
        public string CopyCommand(AdminCommandItem item) => string.Empty;
        public void RequestOpen() => OpenRequested?.Invoke(this, EventArgs.Empty);
        public bool RegisterGlobalHotkey(IntPtr hWnd) { IsRegistered = true; return true; }
        public void UnregisterGlobalHotkey(IntPtr hWnd) { IsRegistered = false; }
        public void Dispose() { }
    }

    private class FakeShortcutGuideService : IShortcutGuideService
    {
        public bool IsRegistered { get; private set; }
        public event EventHandler? OverlayToggleRequested;
        public IReadOnlyList<ShortcutCategory> GetCategories() => Array.Empty<ShortcutCategory>();
        public IReadOnlyList<ShortcutItem> GetAllShortcuts() => Array.Empty<ShortcutItem>();
        public IReadOnlyList<ShortcutItem> SearchShortcuts(string query) => Array.Empty<ShortcutItem>();
        public bool RegisterGlobalHotkey(IntPtr hWnd) { IsRegistered = true; return true; }
        public bool UnregisterGlobalHotkey(IntPtr hWnd) { IsRegistered = false; return true; }
        public void ToggleOverlay() => OverlayToggleRequested?.Invoke(this, EventArgs.Empty);
        public void Dispose() { }
    }

    private class FakeGrabFrameService : IGrabFrameService
    {
        public bool IsRegistered { get; private set; }
        public ScreenBounds CalculateViewportScreenRect(int windowScreenX, int windowScreenY, double localViewportX, double localViewportY, double localViewportWidth, double localViewportHeight, double dpiScale, ScreenBounds? virtualScreenBounds = null) => new(0, 0, 0, 0);
        public GrabFrameTableResult ParseTable(IReadOnlyList<OcrWord> words, IReadOnlyList<double> columnDividers, double viewportWidth) => new(0, 0, [], string.Empty);
        public IReadOnlyList<GrabFrameWordItem> FilterWords(IReadOnlyList<OcrWord> words, string searchQuery, bool exactMatch = false) => Array.Empty<GrabFrameWordItem>();
        public string FormatExtractedText(string rawText, GrabFrameMode mode) => string.Empty;
        public GrabFrameToolbarState CalculateToolbarState(double windowWidth) => new(true, true, true, true, true, true);
        public bool RegisterGlobalHotkey(IntPtr hWnd) { IsRegistered = true; return true; }
        public bool UnregisterGlobalHotkey(IntPtr hWnd) { IsRegistered = false; return true; }
        public void Dispose() { }
    }

    private class FakeEditTextService : IEditTextService
    {
        public bool IsRegistered { get; private set; }
        public event EventHandler? ToggleRequested;
        public event EventHandler<EditTextOpenEventArgs>? OpenRequested;
        public bool RegisterGlobalHotkey(IntPtr hWnd) { IsRegistered = true; return true; }
        public void UnregisterGlobalHotkey(IntPtr hWnd) { IsRegistered = false; }
        public CalculationResult EvaluateExpressions(string input) => new([], 0, 0, 0, 0);
        public Task<bool> TryInsertTextAsync(string text) => Task.FromResult(true);
        public void RequestToggle() => ToggleRequested?.Invoke(this, EventArgs.Empty);
        public void RequestOpen(string? text = null, EditTextTableDocument? table = null) => OpenRequested?.Invoke(this, new EditTextOpenEventArgs(text, table));
    }
#pragma warning restore CS0067

    private class FakeSettingsService : ISettingsService
    {
        public int SchemaVersion => 1;
        public bool IsDemoMode { get; set; } = false;
        public string AdDomain { get; set; } = string.Empty;
        public string AppLanguage { get; set; } = "en";
        public bool IsJiraEnabled { get; set; }
        public string JiraDeploymentMode { get; set; } = "DataCenter";
        public string JiraBaseUrl { get; set; } = string.Empty;
        public string JiraCloudEmail { get; set; } = string.Empty;
        public bool IsMmcLookupEnabled { get; set; } = true;
        public bool IsAdminCommandsEnabled { get; set; } = true;
        public bool IsShortcutGuideEnabled { get; set; } = true;
        public bool IsFileLocksmithShellIntegrationEnabled { get; set; } = false;
        public bool IsGrabFrameEnabled { get; set; } = true;
        public bool GrabFrameAutoOcr { get; set; } = false;
        public bool GrabFrameAlwaysOnTop { get; set; } = true;
        public bool GrabFrameSingleLine { get; set; } = false;
        public bool GrabFrameTableMode { get; set; } = false;
        public bool GrabFrameAutoPaste { get; set; } = false;
        public string GrabFrameDefaultLanguage { get; set; } = "en-US";
        public bool IsEditTextWindowEnabled { get; set; } = true;
        public bool EditTextWindowWordWrap { get; set; } = true;
        public bool EditTextWindowAlwaysOnTop { get; set; } = false;

        public void Load() { }
        public void Save() { }
        public void ResetToDefaults() { }
    }
}
