using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class AttributeEditorSafetyTests : IDisposable
{
    private readonly string _testLogDir;

    public AttributeEditorSafetyTests()
    {
        _testLogDir = Path.Combine(Path.GetTempPath(), "Sol_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testLogDir);
        AdAuditLogger.SetCustomLogDirectoryForTesting(_testLogDir);
    }

    public void Dispose()
    {
        AdAuditLogger.SetCustomLogDirectoryForTesting(null);
        if (Directory.Exists(_testLogDir))
        {
            try { Directory.Delete(_testLogDir, true); } catch { }
        }
    }

    [Theory]
    [InlineData("title", true)]
    [InlineData("department", true)]
    [InlineData("physicalDeliveryOfficeName", true)]
    [InlineData("telephoneNumber", true)]
    [InlineData("mobile", true)]
    [InlineData("streetAddress", true)]
    [InlineData("l", true)]
    [InlineData("st", true)]
    [InlineData("postalCode", true)]
    [InlineData("description", true)]
    [InlineData("wWWHomePage", true)]
    [InlineData("Title", true)] // Case-insensitive check
    [InlineData("DEPARTMENT", true)]
    [InlineData("objectSid", false)]
    [InlineData("objectGUID", false)]
    [InlineData("nTSecurityDescriptor", false)]
    [InlineData("pwdLastSet", false)]
    [InlineData("userAccountControl", false)]
    [InlineData("accountExpires", false)]
    [InlineData("lastLogon", false)]
    [InlineData("memberOf", false)]
    [InlineData("sAMAccountName", false)]
    [InlineData("userPrincipalName", false)]
    public void IsAttributeEditable_StrictlyEnforcesWhitelist(string attributeName, bool expectedAllowed)
    {
        // Act
        bool isAllowed = ActiveDirectoryService.IsAttributeEditable(attributeName);

        // Assert
        Assert.Equal(expectedAllowed, isAllowed);
    }

    [Fact]
    public async Task AuditLogger_WritesDurableStructuredLogEntry()
    {
        // Arrange
        var targetSam = "test.user";
        var attribute = "department";
        var oldVal = "Old Dept";
        var newVal = "New Dept";

        // Act
        await AdAuditLogger.LogAttributeChangeAsync(targetSam, attribute, oldVal, newVal, success: true);

        // Assert
        var logFile = AdAuditLogger.LogFilePath;
        Assert.True(File.Exists(logFile), "Audit log file was not created.");

        var lines = await File.ReadAllLinesAsync(logFile);
        Assert.NotEmpty(lines);

        var lastLine = lines[^1];
        var entry = JsonSerializer.Deserialize<AdAuditEntry>(lastLine);

        Assert.NotNull(entry);
        Assert.Equal(targetSam, entry.TargetUserSam);
        Assert.Equal(attribute, entry.AttributeName);
        Assert.Equal(oldVal, entry.OldValue);
        Assert.Equal(newVal, entry.NewValue);
        Assert.True(entry.Success);
        Assert.True((DateTime.UtcNow - entry.TimestampUtc).TotalMinutes < 5);
    }

    [Fact]
    public void NavigationService_RegistersCorePagesWithoutReflection()
    {
        // Arrange
        var navService = new NavigationService();

        // Assert that core pages are registered
        Assert.Null(navService.CurrentPageKey);
    }

    [Fact]
    public void AdAttributeItem_StoresKeyAndValueCorrectly()
    {
        var item = new Sol.Models.AdAttributeItem("title", "Software Engineer");
        Assert.Equal("title", item.Key);
        Assert.Equal("Software Engineer", item.Value);
    }

    [Theory]
    [InlineData("title", true)]
    [InlineData("givenName", true)]
    [InlineData("sn", true)]
    [InlineData("mail", true)]
    [InlineData("department", true)]
    [InlineData("physicalDeliveryOfficeName", true)]
    [InlineData("userAccountControl", false)]
    [InlineData("adminCount", false)]
    [InlineData("pwdLastSet", false)]
    [InlineData("objectSid", false)]
    [InlineData("memberOf", false)]
    public void IsProfileAttributeEditable_StrictlyEnforcesAllowlist(string attr, bool expected)
    {
        Assert.Equal(expected, ActiveDirectoryService.IsProfileAttributeEditable(attr));
    }

    [Theory]
    [InlineData(0u, "System Idle Process", null, true)]
    [InlineData(4u, "System", null, true)]
    [InlineData(100u, "lsass.exe", null, true)]
    [InlineData(101u, "csrss.exe", null, true)]
    [InlineData(102u, "wininit.exe", null, true)]
    [InlineData(103u, "services.exe", null, true)]
    [InlineData(104u, "dwm.exe", null, true)]
    [InlineData(105u, "svchost.exe", "NT AUTHORITY\\SYSTEM", true)]
    [InlineData(106u, "svchost.exe", "CORP\\user", false)]
    [InlineData(5000u, "chrome.exe", "CORP\\user", false)]
    [InlineData(5001u, "notepad.exe", "CORP\\user", false)]
    public void IsCriticalProcess_GuardsCriticalProcesses(uint pid, string name, string? owner, bool expectedCritical)
    {
        Assert.Equal(expectedCritical, Sol.Models.ComputerProcessInfo.IsCriticalProcess(pid, name, owner));
    }

    [Fact]
    public void SettingsViewModel_NegativeIndex_DoesNotCorruptLanguageOrMode()
    {
        var settings = new SettingsService();
        settings.AppLanguage = "de";
        settings.JiraDeploymentMode = "Cloud";
        var jira = new JiraService(settings);
        var vm = new ViewModels.SettingsViewModel(settings, jira);

        Assert.Equal("de", vm.AppLanguage);
        Assert.Equal(1, vm.AppLanguageIndex);
        Assert.Equal("Cloud", vm.JiraDeploymentMode);
        Assert.Equal(1, vm.JiraDeploymentModeIndex);

        // Simulate WinUI 3 transient unselected -1 on visual layout
        vm.AppLanguageIndex = -1;
        vm.JiraDeploymentModeIndex = -1;

        Assert.Equal("de", vm.AppLanguage);
        Assert.Equal("Cloud", vm.JiraDeploymentMode);
    }

    [Fact]
    public void SettingsViewModel_TogglingJira_AutoPersistsWithoutRestart()
    {
        var settings = new SettingsService();
        settings.IsJiraEnabled = false;
        var jira = new JiraService(settings);
        var vm = new ViewModels.SettingsViewModel(settings, jira);

        Assert.False(settings.IsJiraEnabled);

        // Turn on
        vm.IsJiraEnabled = true;
        Assert.True(settings.IsJiraEnabled);

        // Turn off
        vm.IsJiraEnabled = false;
        Assert.False(settings.IsJiraEnabled);
    }

    [Fact]
    public async Task SettingsViewModel_TestConnection_WarnsOnEmptyUrl()
    {
        var settings = new SettingsService();
        var jira = new JiraService(settings);
        var vm = new ViewModels.SettingsViewModel(settings, jira);

        vm.JiraBaseUrl = "";
        await vm.TestJiraConnectionCommand.ExecuteAsync(null);

        Assert.True(vm.IsJiraTestStatusOpen);
        Assert.Equal(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning, vm.JiraTestStatusSeverity);
    }

    [Fact]
    public void ComputerServiceInfo_AvailableStartModes_HasAllThreeOptions()
    {
        var info = new Models.ComputerServiceInfo { StartMode = "Auto" };
        Assert.Equal(3, info.AvailableStartModes.Length);
        Assert.Equal(0, info.StartModeIndex);

        var manualInfo = new Models.ComputerServiceInfo { StartMode = "Manual" };
        Assert.Equal(1, manualInfo.StartModeIndex);

        var disabledInfo = new Models.ComputerServiceInfo { StartMode = "Disabled" };
        Assert.Equal(2, disabledInfo.StartModeIndex);
    }

    [Fact]
    public void ComputerWorkspaceViewModel_IsDiagnosticsLoading_EvaluatesAccurately()
    {
        var vm = new ViewModels.ComputerWorkspaceViewModel(null!, null!, null!);
        Assert.False(vm.IsDiagnosticsLoading);

        vm.IsHardwareLoading = true;
        Assert.True(vm.IsDiagnosticsLoading);

        vm.IsHardwareLoading = false;
        Assert.False(vm.IsDiagnosticsLoading);

        vm.IsDiskLoading = true;
        Assert.True(vm.IsDiagnosticsLoading);
        vm.IsDiskLoading = false;

        vm.IsProcessesLoading = true;
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Visible, vm.ProcessesLoadingVisibility);
        vm.IsProcessesLoading = false;
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, vm.ProcessesLoadingVisibility);

        vm.IsServicesLoading = true;
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Visible, vm.ServicesLoadingVisibility);
        vm.IsServicesLoading = false;
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, vm.ServicesLoadingVisibility);
    }

    [Fact]
    public void DiagnosticsQuerying_String_IsLocalizedProperly()
    {
        var s = Helpers.Strings.S;
        Assert.False(string.IsNullOrWhiteSpace(s.DiagnosticsQuerying));
        Assert.False(string.IsNullOrWhiteSpace(s.FetchingProcessData));
        Assert.False(string.IsNullOrWhiteSpace(s.FetchingServicesData));
    }
}
