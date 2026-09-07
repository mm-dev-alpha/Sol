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

        // Assert that core pages are registered and mapped to correct view types
        Assert.Null(navService.CurrentPageKey);
        Assert.True(navService.RegisteredPages.ContainsKey("HomePage"));
        Assert.Equal(typeof(Sol.Views.HomePage), navService.RegisteredPages["HomePage"]);
        Assert.Equal(typeof(Sol.Views.UserWorkspacePage), navService.RegisteredPages["UserWorkspacePage"]);
        Assert.Equal(typeof(Sol.Views.ComputerWorkspacePage), navService.RegisteredPages["ComputerWorkspacePage"]);
        Assert.Equal(typeof(Sol.Views.JiraWorkspacePage), navService.RegisteredPages["JiraWorkspacePage"]);
        Assert.Equal(typeof(Sol.Views.ToolsPage), navService.RegisteredPages["ToolsPage"]);
        Assert.Equal(typeof(Sol.Views.CompareWorkspacePage), navService.RegisteredPages["CompareWorkspacePage"]);
        Assert.Equal(typeof(Sol.Views.SettingsPage), navService.RegisteredPages["SettingsPage"]);
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
    public void SettingsViewModel_NegativeIndex_DoesNotCorruptDeploymentMode()
    {
        var settings = new SettingsService();
        settings.JiraDeploymentMode = "Cloud";
        var jira = new JiraService(settings);
        var vm = new ViewModels.SettingsViewModel(settings, jira);

        Assert.Equal("Cloud", vm.JiraDeploymentMode);
        Assert.Equal(1, vm.JiraDeploymentModeIndex);

        // Simulate WinUI 3 transient unselected -1 on visual layout
        vm.JiraDeploymentModeIndex = -1;

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
    public async Task SettingsViewModel_TestConnection_WarnsOnMissingSecret()
    {
        var settings = new SettingsService();
        var jira = new JiraService(settings);
        var vm = new ViewModels.SettingsViewModel(settings, jira);

        vm.JiraBaseUrl = "https://jira.company.local";
        vm.JiraDeploymentMode = "DataCenter";
        vm.JiraPatSecret = "";
        await vm.TestJiraConnectionCommand.ExecuteAsync(null);

        Assert.True(vm.IsJiraTestStatusOpen);
        Assert.Equal(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning, vm.JiraTestStatusSeverity);
    }

    [Fact]
    public async Task SettingsViewModel_TestConnection_WarnsOnCloudMissingEmail()
    {
        var settings = new SettingsService();
        var jira = new JiraService(settings);
        var vm = new ViewModels.SettingsViewModel(settings, jira);

        vm.JiraBaseUrl = "https://company.atlassian.net";
        vm.JiraDeploymentMode = "Cloud";
        vm.JiraCloudEmail = "";
        vm.JiraCloudTokenSecret = "some-token";
        await vm.TestJiraConnectionCommand.ExecuteAsync(null);

        Assert.True(vm.IsJiraTestStatusOpen);
        Assert.Equal(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning, vm.JiraTestStatusSeverity);
    }

    [Fact]
    public async Task JiraService_TestConnectionAsync_ReturnsFalseForInvalidUrlOrEmptySecret()
    {
        var settings = new SettingsService();
        var jira = new JiraService(settings);

        // Empty secret
        bool res1 = await jira.TestConnectionAsync("https://jira.example.com", "DataCenter", "", "");
        Assert.False(res1);

        // Invalid non-HTTP URL
        bool res2 = await jira.TestConnectionAsync("not-a-valid-url", "DataCenter", "", "secret");
        Assert.False(res2);

        // Cloud missing email
        bool res3 = await jira.TestConnectionAsync("https://company.atlassian.net", "Cloud", "", "secret");
        Assert.False(res3);

        // SEC-HIGH-05: Insecure HTTP transport strictly rejected
        bool res4 = await jira.TestConnectionAsync("http://jira.example.com", "DataCenter", "", "secret");
        Assert.False(res4);
    }

    [Theory]
    [InlineData("wuauserv", true)]
    [InlineData("Spooler", true)]
    [InlineData("MSSQL$INSTANCE", true)]
    [InlineData("App_Service-1.0", true)]
    [InlineData("Spooler' OR 1=1 --", false)]
    [InlineData("Spooler; DROP TABLE", false)]
    [InlineData("service\"name", false)]
    [InlineData("service\\name", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ProcessManagementService_IsValidServiceName_ValidatesProperly(string? serviceName, bool expected)
    {
        Assert.Equal(expected, Services.ProcessManagementService.IsValidServiceName(serviceName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void FileLocksmithService_KillProcess_GuardsSystemPids(int pid)
    {
        var service = new Services.FileLocksmithService();
        bool killed = service.KillProcess(pid, out string? errorMessage);
        Assert.False(killed);
        Assert.Equal(Helpers.Strings.S.CriticalProcessCannotBeTerminated, errorMessage);
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
        Assert.True(vm.IsProcessesLoading);
        vm.IsProcessesLoading = false;
        Assert.False(vm.IsProcessesLoading);

        vm.IsServicesLoading = true;
        Assert.True(vm.IsServicesLoading);
        vm.IsServicesLoading = false;
        Assert.False(vm.IsServicesLoading);
    }

    [Fact]
    public void DiagnosticsQuerying_String_IsLocalizedProperly()
    {
        var s = Helpers.Strings.S;
        Assert.False(string.IsNullOrWhiteSpace(s.DiagnosticsQuerying));
        Assert.False(string.IsNullOrWhiteSpace(s.FetchingProcessData));
        Assert.False(string.IsNullOrWhiteSpace(s.FetchingServicesData));
    }

    [Fact]
    public void GenerateSecurePassword_MeetsLengthAndComplexityRequirements()
    {
        const string uppers = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowers = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string specials = "!@#$%^&*-_+=";

        var generatedPasswords = new HashSet<string>();

        for (int i = 0; i < 50; i++)
        {
            string password = ViewModels.UserWorkspaceViewModel.GenerateSecurePassword();
            Assert.Equal(16, password.Length);
            Assert.Contains(password, c => uppers.Contains(c));
            Assert.Contains(password, c => lowers.Contains(c));
            Assert.Contains(password, c => digits.Contains(c));
            Assert.Contains(password, c => specials.Contains(c));
            generatedPasswords.Add(password);
        }

        // Entropy check: 50 randomly generated passwords should all be distinct
        Assert.Equal(50, generatedPasswords.Count);
    }

    [Theory]
    [InlineData("manager", false)]
    [InlineData("distinguishedName", false)]
    [InlineData("sAMAccountName", false)]
    [InlineData("objectGUID", false)]
    public void IsProfileAttributeEditable_DoesNotPermitManagerOrStructuralAttributesDirectly(string attr, bool expected)
    {
        Assert.Equal(expected, ActiveDirectoryService.IsProfileAttributeEditable(attr));
    }
}
