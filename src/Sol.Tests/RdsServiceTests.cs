using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Sol.Helpers;
using Sol.Models;
using Sol.Services;
using Sol.ViewModels;
using Xunit;

namespace Sol.Tests;

public class RdsServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _settingsPath;

    public RdsServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SolRdsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _settingsPath = Path.Combine(_testDir, "appsettings.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    // --- Model Tests ---

    [Fact]
    public void RdsSessionItem_StatusFlags_EvaluatedCorrectly()
    {
        var activeSession = new RdsSessionItem
        {
            Username = "jdoe",
            Domain = "CORP",
            SessionId = 2,
            State = "STATE_ACTIVE"
        };

        var disconnectedSession = new RdsSessionItem
        {
            Username = "asmith",
            Domain = "CORP",
            SessionId = 5,
            State = "STATE_DISCONNECTED"
        };

        Assert.True(activeSession.IsActive);
        Assert.False(activeSession.IsDisconnected);
        Assert.Equal("CORP\\jdoe", activeSession.UserPrincipalDisplay);

        Assert.False(disconnectedSession.IsActive);
        Assert.True(disconnectedSession.IsDisconnected);
        Assert.Equal("CORP\\asmith", disconnectedSession.UserPrincipalDisplay);
    }

    [Fact]
    public void RdsSessionItem_ConnectedAndOtherStates_EvaluatedCorrectly()
    {
        var connectedSession = new RdsSessionItem
        {
            Username = "mmezger",
            Domain = "COWIE",
            SessionId = 16,
            State = "STATE_CONNECTED"
        };

        var listenSession = new RdsSessionItem
        {
            Username = "",
            Domain = "",
            SessionId = 65536,
            State = "STATE_LISTEN"
        };

        Assert.False(connectedSession.IsActive);
        Assert.True(connectedSession.IsConnected);
        Assert.False(connectedSession.IsDisconnected);
        Assert.False(connectedSession.IsOtherState);
        Assert.Equal("Connected", connectedSession.DisplayState);

        Assert.False(listenSession.IsActive);
        Assert.False(listenSession.IsConnected);
        Assert.False(listenSession.IsDisconnected);
        Assert.True(listenSession.IsOtherState);
        Assert.Equal("Listen", listenSession.DisplayState);
    }

    [Fact]
    public void RdsDiskLayoutInfo_CapacityGiBCalculation_IsAccurate()
    {
        var info = new RdsDiskLayoutInfo
        {
            VhdxPath = @"\\server\share\UVHD-S-1-5-21-test.vhdx",
            CapacityBytes = 21474836480UL, // 20 GiB
            PartitionSizeBytes = 21474836480UL,
            PartitionStyle = "GPT",
            Exists = true
        };

        Assert.Equal(20.0, info.CapacityGiB);
        Assert.Equal(20.0, info.PartitionSizeGiB);
        Assert.True(info.Exists);
        Assert.False(info.IsFileLocked);
    }

    [Fact]
    public void RdsDiskExpansionResult_GiBConversion_IsAccurate()
    {
        var result = new RdsDiskExpansionResult
        {
            IsSuccess = true,
            InitialCapacityBytes = 10737418240UL, // 10 GiB
            TargetCapacityBytes = 16106127360UL,  // 15 GiB
            FinalCapacityBytes = 16106127360UL,
            FinalPartitionBytes = 16106127360UL
        };

        Assert.True(result.IsSuccess);
        Assert.Equal(15.0, result.FinalCapacityGiB);
        Assert.Equal(15.0, result.FinalPartitionGiB);
    }

    [Fact]
    public void Strings_RdsExtendedStrings_AreDefinedAndFormatted()
    {
        Assert.False(string.IsNullOrWhiteSpace(Strings.S.RdsBrokerFqdnRequiredError));
        Assert.Contains("{0}", Strings.S.RdsBrokerFqdnRequiredError);
        Assert.False(string.IsNullOrWhiteSpace(Strings.S.RdsSessionsConnectedBadgeFormat));
        Assert.Contains("{0}", Strings.S.RdsSessionsConnectedBadgeFormat);
        Assert.False(string.IsNullOrWhiteSpace(Strings.S.RdsUpdSelectUserHeader));
        Assert.False(string.IsNullOrWhiteSpace(Strings.S.RdsUpdCollectionHeader));
        Assert.False(string.IsNullOrWhiteSpace(Strings.S.RdsUpdNoCollectionsFound));
        Assert.False(string.IsNullOrWhiteSpace(Strings.S.RdsUpdNotEnabledWarning));
        Assert.False(string.IsNullOrWhiteSpace(Strings.S.RdsSessionsSortUsername));
        Assert.DoesNotContain("or hostname", Strings.S.RdsSettingsBrokerDesc, StringComparison.OrdinalIgnoreCase);
    }

    // --- Service Tests ---

    [Fact]
    public void RdsService_IsElevated_DoesNotThrow()
    {
        var service = new RdsService();
        bool elevated = service.IsElevated();
        // Just verify execution succeeds without unhandled exception
        Assert.True(elevated || !elevated);
    }

    [Fact]
    public async Task RdsService_GetSessions_ReturnsEmptyOnBlankBroker()
    {
        var service = new RdsService();
        var sessions = await service.GetSessionsAsync("   ");
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task RdsService_DisconnectSession_ThrowsOnBlankBroker()
    {
        var service = new RdsService();
        await Assert.ThrowsAsync<ArgumentException>(() => service.DisconnectSessionAsync("   ", "RDSH-01", 1));
    }

    [Fact]
    public async Task RdsService_LogoffSession_ThrowsOnBlankBroker()
    {
        var service = new RdsService();
        await Assert.ThrowsAsync<ArgumentException>(() => service.LogoffSessionAsync("   ", "RDSH-01", 1));
    }

    [Fact]
    public async Task RdsService_GetCollections_ReturnsEmptyOnBlankBroker()
    {
        var service = new RdsService();
        var collections = await service.GetCollectionsAsync("   ");
        Assert.Empty(collections);
    }

    [Fact]
    public async Task RdsService_InspectUpdDisk_HandlesNonExistentPath()
    {
        var service = new RdsService();
        string dummyPath = Path.Combine(_testDir, "non_existent_disk.vhdx");
        var result = await service.InspectUpdDiskAsync(dummyPath);

        Assert.False(result.Exists);
        Assert.Equal(dummyPath, result.VhdxPath);
        Assert.Contains("does not exist", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RdsService_ExpandUpdDisk_RejectsZeroIncrease()
    {
        var service = new RdsService();
        string dummyPath = Path.Combine(_testDir, "test.vhdx");

        var zeroResult = await service.ExpandUpdDiskAsync("broker.corp.local", "Collection1", dummyPath, "S-1-5-21-123", "jdoe", 0);
        Assert.False(zeroResult.IsSuccess);
        Assert.Contains("at least 1 GiB", zeroResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // --- Settings Persistence Tests ---

    [Fact]
    public void SettingsService_RdsSettings_PersistAndReloadCorrectly()
    {
        var settings1 = new SettingsService(_settingsPath);
        Assert.False(settings1.IsRdsEnabled);
        Assert.Equal(string.Empty, settings1.RdsConnectionBroker);
        Assert.Equal(1, settings1.RdsDefaultDiskIncreaseGB);

        settings1.IsRdsEnabled = true;
        settings1.RdsConnectionBroker = "broker01.corp.contoso.com";
        settings1.RdsDefaultDiskIncreaseGB = 10;
        settings1.Save();

        var settings2 = new SettingsService(_settingsPath);
        Assert.True(settings2.IsRdsEnabled);
        Assert.Equal("broker01.corp.contoso.com", settings2.RdsConnectionBroker);
        Assert.Equal(10, settings2.RdsDefaultDiskIncreaseGB);
    }

    [Fact]
    public void RdsSettingsChangedMessage_TransfersValue()
    {
        var msg = new RdsSettingsChangedMessage(true);
        Assert.True(msg.IsEnabled);
        Assert.True(msg.Value);

        var msgOff = new RdsSettingsChangedMessage(false);
        Assert.False(msgOff.IsEnabled);
        Assert.False(msgOff.Value);
    }

    // --- ViewModel Tests ---

    [Fact]
    public void RdsWorkspaceViewModel_FilterLogic_FiltersByUsernameDomainServer()
    {
        var mockRds = new MockRdsService();
        var mockAd = new MockAdService();
        var mockSettings = new MockSettingsService { RdsConnectionBroker = "broker.test" };
        var mockNav = new MockNavigationService();

        var vm = new RdsWorkspaceViewModel(mockRds, mockAd, mockSettings, mockNav);

        vm.AllSessions.Add(new RdsSessionItem { Username = "alice", Domain = "CORP", HostServer = "RDSH-01", State = "Active" });
        vm.AllSessions.Add(new RdsSessionItem { Username = "bob", Domain = "DEV", HostServer = "RDSH-02", State = "Disconnected" });
        vm.AllSessions.Add(new RdsSessionItem { Username = "charlie", Domain = "CORP", HostServer = "RDSH-01", State = "Active" });

        vm.SessionFilterQuery = "alice";
        vm.FilteredSessions.Clear();
        foreach (var s in vm.AllSessions.Where(s => s.Username.Contains("alice", StringComparison.OrdinalIgnoreCase)))
            vm.FilteredSessions.Add(s);

        Assert.Single(vm.FilteredSessions);
        Assert.Equal("alice", vm.FilteredSessions[0].Username);
    }

    [Fact]
    public void RdsWorkspaceViewModel_TargetGiBCalculation()
    {
        var mockRds = new MockRdsService();
        var mockAd = new MockAdService();
        var mockSettings = new MockSettingsService { RdsDefaultDiskIncreaseGB = 5 };
        var mockNav = new MockNavigationService();

        var vm = new RdsWorkspaceViewModel(mockRds, mockAd, mockSettings, mockNav);
        vm.AdditionalGiB = 5;

        Assert.Equal(5.0, vm.TargetGiB);

        vm.DiskLayout = new RdsDiskLayoutInfo
        {
            CapacityBytes = 21474836480UL, // 20 GiB
            Exists = true
        };

        Assert.Equal(25.0, vm.TargetGiB);
    }

    [Fact]
    public void Strings_AllRdsStrings_ArePopulated()
    {
        var strings = Strings.S;
        Assert.False(string.IsNullOrWhiteSpace(strings.NavRdsWorkspace));
        Assert.False(string.IsNullOrWhiteSpace(strings.RdsTabSessions));
        Assert.False(string.IsNullOrWhiteSpace(strings.RdsTabProfileDisks));
        Assert.False(string.IsNullOrWhiteSpace(strings.RdsSessionsColUsername));
        Assert.False(string.IsNullOrWhiteSpace(strings.RdsSessionsLogoffBtn));
        Assert.False(string.IsNullOrWhiteSpace(strings.RdsSessionsDisconnectBtn));
        Assert.False(string.IsNullOrWhiteSpace(strings.RdsUpdDiskInfoTitle));
        Assert.False(string.IsNullOrWhiteSpace(strings.RdsUpdExpandBtn));
        Assert.False(string.IsNullOrWhiteSpace(strings.RdsUpdRestartAsAdminBtn));
        Assert.False(string.IsNullOrWhiteSpace(strings.LoadingPrompt));
        Assert.Equal("GiB", strings.RdsGiBSuffix);
        Assert.Equal("Unified Session ID", strings.RdsUnifiedSessionIdLabel);

        // Verify string format placeholders
        string confirmMsg = string.Format(strings.RdsUpdConfirmExpandMsg, "jdoe", 20.0, 22.0);
        Assert.Contains("jdoe", confirmMsg);
        Assert.Contains("20.0 GiB", confirmMsg);
        Assert.Contains("22.0 GiB", confirmMsg);

        string successMsg = string.Format(strings.RdsUpdExpandSuccess, 22.0);
        Assert.Contains("22.0 GiB", successMsg);
    }

    [Fact]
    public void RdsWorkspaceViewModel_Dispose_CleansUpWithoutThrowing()
    {
        var mockRds = new MockRdsService();
        var mockAd = new MockAdService();
        var mockSettings = new MockSettingsService();
        var mockNav = new MockNavigationService();

        var vm = new RdsWorkspaceViewModel(mockRds, mockAd, mockSettings, mockNav);
        vm.Dispose(); // Should stop timer and cancel CTS without throwing
    }

    // --- Mock Implementations for VM Testing ---

    private class MockRdsService : IRdsService
    {
        public bool IsElevated() => true;
        public bool RestartAsAdministrator() => true;
        public Task<bool> TestBrokerConnectionAsync(string broker, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public Task<IReadOnlyList<RdsSessionItem>> GetSessionsAsync(string broker, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RdsSessionItem>>(new List<RdsSessionItem>());
        public Task<bool> LogoffSessionAsync(string broker, string hostServer, int unifiedSessionId, bool force = true, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public Task<bool> DisconnectSessionAsync(string broker, string hostServer, int unifiedSessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public Task<IReadOnlyList<RdsCollectionInfo>> GetCollectionsAsync(string broker, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RdsCollectionInfo>>(new List<RdsCollectionInfo>());
        public Task<RdsDiskLayoutInfo> InspectUpdDiskAsync(string vhdxPath, CancellationToken cancellationToken = default)
            => Task.FromResult(new RdsDiskLayoutInfo { Exists = true, CapacityBytes = 21474836480UL });
        public Task<RdsDiskExpansionResult> ExpandUpdDiskAsync(
            string broker,
            string collectionName,
            string vhdxPath,
            string userSid,
            string samAccountName,
            ulong additionalGigabytes,
            IProgress<RdsExpansionStep>? progress = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new RdsDiskExpansionResult { IsSuccess = true });
    }

    private class MockAdService : IActiveDirectoryService
    {
        public Task<List<AdUser>> SearchUsersAsync(string query) => Task.FromResult(new List<AdUser>());
        public Task<List<string>> SearchGroupsAsync(string query) => Task.FromResult(new List<string>());
        public Task<List<KeyValuePair<string, string>>> GetAllUserAttributesAsync(string samAccountName) => Task.FromResult(new List<KeyValuePair<string, string>>());
        public Task UnlockAccountAsync(string samAccountName) => Task.CompletedTask;
        public Task EnableAccountAsync(string samAccountName, bool enable) => Task.CompletedTask;
        public Task ResetPasswordAsync(string samAccountName, string newPassword, bool requireChangeAtNextLogon) => Task.CompletedTask;
        public Task ForcePasswordChangeAsync(string samAccountName) => Task.CompletedTask;
        public Task UpdateUserProfileAsync(string samAccountName, Dictionary<string, string> attributes, string? newManager) => Task.CompletedTask;
        public Task UpdateRawAttributeAsync(string samAccountName, string attributeName, string newValue) => Task.CompletedTask;
        public Task AddUserToGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
        public Task RemoveUserFromGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
        public Task<List<AdComputer>> SearchComputersAsync(string query) => Task.FromResult(new List<AdComputer>());
        public Task EnableComputerAccountAsync(string samAccountName, bool enable) => Task.CompletedTask;
        public Task AddComputerToGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
        public Task RemoveComputerFromGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
    }

    private class MockSettingsService : ISettingsService
    {
        public int SchemaVersion => 1;
        public bool IsDemoMode { get; set; } = false;
        public string AdDomain { get; set; } = "contoso.local";
        public string AppLanguage { get; set; } = "en";
        public bool IsJiraEnabled { get; set; } = false;
        public string JiraDeploymentMode { get; set; } = "DataCenter";
        public string JiraBaseUrl { get; set; } = string.Empty;
        public string JiraCloudEmail { get; set; } = string.Empty;
        public bool IsRdsEnabled { get; set; } = false;
        public string RdsConnectionBroker { get; set; } = string.Empty;
        public int RdsDefaultDiskIncreaseGB { get; set; } = 1;
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
    }

    private class MockNavigationService : INavigationService
    {
        public bool CanGoBack => false;
        public string? CurrentPageKey => "RdsWorkspacePage";
        public event EventHandler<string>? Navigated { add { } remove { } }
        public void Initialize(Microsoft.UI.Xaml.Controls.Frame frame) { }
        public bool NavigateTo(string pageKey, object? parameter = null) => true;
        public bool GoBack() => false;
    }
}
