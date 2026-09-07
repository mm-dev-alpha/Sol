using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Models;
using Sol.Services;
using Sol.ViewModels;
using Xunit;

namespace Sol.Tests;

public class ToolsSettingsTests : IDisposable
{
    private readonly string _testSettingsDir;
    private readonly string _testSettingsFile;

    public ToolsSettingsTests()
    {
        _testSettingsDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Sol_Settings_Tests_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_testSettingsDir);
        _testSettingsFile = System.IO.Path.Combine(_testSettingsDir, "appsettings.json");
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Reset();
        if (System.IO.Directory.Exists(_testSettingsDir))
        {
            try { System.IO.Directory.Delete(_testSettingsDir, true); } catch { }
        }
    }

    private class TestSettingsService : ISettingsService
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

        public int SaveCallCount { get; private set; }

        public void Load() { }
        public void Save() => SaveCallCount++;
    }

    private class TestJiraService : IJiraService
    {
        public Task<List<JiraTicket>> GetTicketsCreatedByUserAsync(string userEmailOrSam, int startAt = 0, int maxResults = 10, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<JiraTicket>());

        public Task<bool> TestConnectionAsync(string? overrideBaseUrl = null, string? overrideMode = null, string? overrideEmail = null, string? overrideSecret = null, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private class TestFileLocksmithService : IFileLocksmithService
    {
        public bool RegisteredState { get; set; }
        public int SetCallCount { get; private set; }

        public Task<IReadOnlyList<LockingProcessInfo>> FindLockingProcessesAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LockingProcessInfo>>([]);
        public Task<IReadOnlyList<LockingProcessInfo>> FindLockingProcessesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LockingProcessInfo>>([]);
        public bool KillProcess(int processId, out string? errorMessage) { errorMessage = null; return true; }
        public bool KillAllProcesses(IEnumerable<int> processIds, out List<string> errors) { errors = []; return true; }
        public bool IsContextMenuRegistered() => RegisteredState;
        public bool SetContextMenuRegistered(bool enable) { RegisteredState = enable; SetCallCount++; return true; }
    }

    [Fact]
    public void SettingsService_LoadsAndSavesToolsConfiguration()
    {
        var settings = new SettingsService(_testSettingsFile);
        
        // Assert initial defaults
        Assert.True(settings.IsMmcLookupEnabled);
        Assert.True(settings.IsAdminCommandsEnabled);
        Assert.True(settings.IsShortcutGuideEnabled);
        Assert.False(settings.IsFileLocksmithShellIntegrationEnabled);
        Assert.True(settings.IsGrabFrameEnabled);
        Assert.False(settings.GrabFrameAutoOcr);
        Assert.True(settings.GrabFrameAlwaysOnTop);

        // Modify settings
        settings.IsMmcLookupEnabled = false;
        settings.IsAdminCommandsEnabled = false;
        settings.IsShortcutGuideEnabled = false;
        settings.IsFileLocksmithShellIntegrationEnabled = true;
        settings.IsGrabFrameEnabled = false;
        settings.GrabFrameAutoOcr = true;
        settings.GrabFrameAlwaysOnTop = false;

        settings.Save();

        // Reload from isolated test file
        var reloadedSettings = new SettingsService(_testSettingsFile);
        Assert.False(reloadedSettings.IsMmcLookupEnabled);
        Assert.False(reloadedSettings.IsAdminCommandsEnabled);
        Assert.False(reloadedSettings.IsShortcutGuideEnabled);
        Assert.True(reloadedSettings.IsFileLocksmithShellIntegrationEnabled);
        Assert.False(reloadedSettings.IsGrabFrameEnabled);
        Assert.True(reloadedSettings.GrabFrameAutoOcr);
        Assert.False(reloadedSettings.GrabFrameAlwaysOnTop);

        // Cleanup modified settings file back to default
        reloadedSettings.IsMmcLookupEnabled = true;
        reloadedSettings.IsAdminCommandsEnabled = true;
        reloadedSettings.IsShortcutGuideEnabled = true;
        reloadedSettings.IsFileLocksmithShellIntegrationEnabled = false;
        reloadedSettings.IsGrabFrameEnabled = true;
        reloadedSettings.GrabFrameAutoOcr = false;
        reloadedSettings.GrabFrameAlwaysOnTop = true;
        reloadedSettings.Save();
    }

    [Fact]
    public void SettingsViewModel_InitializesFromServices()
    {
        var settings = new TestSettingsService
        {
            IsMmcLookupEnabled = true,
            IsAdminCommandsEnabled = false,
            IsShortcutGuideEnabled = false
        };

        var jira = new TestJiraService();
        var locksmith = new TestFileLocksmithService { RegisteredState = true };

        var vm = new SettingsViewModel(settings, jira, locksmith);

        Assert.True(vm.IsMmcLookupEnabled);
        Assert.False(vm.IsAdminCommandsEnabled);
        Assert.False(vm.IsShortcutGuideEnabled);
        Assert.True(vm.IsFileLocksmithShellIntegrationEnabled);
    }

    [Fact]
    public void SettingsViewModel_Save_UpdatesSettingsAndBroadcastsMessage()
    {
        var settings = new TestSettingsService();
        var jira = new TestJiraService();
        var locksmith = new TestFileLocksmithService();

        var vm = new SettingsViewModel(settings, jira, locksmith);

        vm.IsMmcLookupEnabled = false;
        vm.IsAdminCommandsEnabled = false;
        vm.IsShortcutGuideEnabled = true;
        vm.IsFileLocksmithShellIntegrationEnabled = true;

        ToolsSettingsChangedMessage? receivedMessage = null;
        WeakReferenceMessenger.Default.Register<ToolsSettingsChangedMessage>(this, (r, m) =>
        {
            receivedMessage = m;
        });

        vm.SaveCommand.Execute(null);

        Assert.False(settings.IsMmcLookupEnabled);
        Assert.False(settings.IsAdminCommandsEnabled);
        Assert.True(settings.IsShortcutGuideEnabled);
        Assert.True(settings.IsFileLocksmithShellIntegrationEnabled);
        Assert.True(settings.SaveCallCount > 0);

        Assert.Equal(1, locksmith.SetCallCount);
        Assert.True(locksmith.RegisteredState);

        Assert.NotNull(receivedMessage);
        Assert.False(receivedMessage!.IsMmcLookupEnabled);
        Assert.False(receivedMessage!.IsAdminCommandsEnabled);
        Assert.True(receivedMessage!.IsShortcutGuideEnabled);
    }

    [Fact]
    public void MmcLookupService_HotkeyUnregister_HandlesZeroHandleCleanly()
    {
        using var service = new MmcLookupService();
        service.UnregisterGlobalHotkey(IntPtr.Zero);
        Assert.True(true);
    }

    [Fact]
    public void AdminCommandService_HotkeyUnregister_HandlesZeroHandleCleanly()
    {
        using var service = new AdminCommandService();
        service.UnregisterGlobalHotkey(IntPtr.Zero);
        Assert.True(true);
    }

    [Fact]
    public void ShortcutGuideService_HotkeyUnregister_HandlesZeroHandleCleanly()
    {
        var service = new ShortcutGuideService();
        bool result = service.UnregisterGlobalHotkey(IntPtr.Zero);
        Assert.True(result);
    }

    [Fact]
    public void GrabFrameService_HotkeyUnregister_HandlesZeroHandleCleanly()
    {
        using var service = new GrabFrameService();
        bool result = service.UnregisterGlobalHotkey(IntPtr.Zero);
        Assert.True(result);
    }

    [Fact]
    public void EditTextService_HotkeyUnregister_HandlesZeroHandleCleanly()
    {
        using var service = new EditTextService();
        service.UnregisterGlobalHotkey(IntPtr.Zero);
        Assert.True(true);
    }

    [Fact]
    public void SettingsViewModel_EditTextWindowSettings_TriggersSave()
    {
        var settings = new TestSettingsService();
        var vm = new SettingsViewModel(settings, new TestJiraService(), new TestFileLocksmithService());

        vm.IsEditTextWindowEnabled = false;
        vm.EditTextWindowWordWrap = false;
        vm.EditTextWindowAlwaysOnTop = true;

        vm.SaveCommand.Execute(null);

        Assert.False(settings.IsEditTextWindowEnabled);
        Assert.False(settings.EditTextWindowWordWrap);
        Assert.True(settings.EditTextWindowAlwaysOnTop);
        Assert.True(settings.SaveCallCount >= 1);
    }
}
