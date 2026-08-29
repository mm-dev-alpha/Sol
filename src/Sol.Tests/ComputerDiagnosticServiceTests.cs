using System;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;
using Sol.Services;
using Sol.ViewModels;
using Xunit;

namespace Sol.Tests;

public class ComputerDiagnosticServiceTests
{
    [Theory]
    [InlineData("26100", "24H2")]
    [InlineData("22631", "23H2")]
    [InlineData("22621", "22H2")]
    [InlineData("22000", "21H2")]
    [InlineData("19045", "22H2")]
    [InlineData("19044", "21H2")]
    [InlineData("19042", "20H2")]
    [InlineData("17763", "1809")]
    [InlineData("14393", "1607")]
    [InlineData("", "")]
    [InlineData("invalid", "")]
    public void GetWindowsDisplayVersionFromBuild_MapsCorrectly(string build, string expectedDisplay)
    {
        var result = ComputerDiagnosticService.GetWindowsDisplayVersionFromBuild(build);
        Assert.Equal(expectedDisplay, result);
    }

    [Theory]
    [InlineData("Dell Inc.", "7G8X9Y2", "https://www.dell.com/support/home/product-support/servicetag/7G8X9Y2/overview")]
    [InlineData("Lenovo", "PF3A4B5C", "https://pcsupport.lenovo.com/products/search?query=PF3A4B5C")]
    [InlineData("HP", "5CD1234XYZ", "https://support.hp.com/us-en/checkwarranty?serialnumber=5CD1234XYZ")]
    [InlineData("Hewlett-Packard", "5CD1234XYZ", "https://support.hp.com/us-en/checkwarranty?serialnumber=5CD1234XYZ")]
    [InlineData("Microsoft Corporation", "0123456789", null)]
    [InlineData("Dell Inc.", "", null)]
    [InlineData("Dell Inc.", "   ", null)]
    public void GetWarrantyUrl_GeneratesValidVendorUrls(string manufacturer, string serial, string? expectedUrl)
    {
        var service = new ComputerDiagnosticService();
        var url = service.GetWarrantyUrl(manufacturer, serial);
        Assert.Equal(expectedUrl, url);
    }

    [Fact]
    public async Task GetHardwareSnapshotAsync_EmptyHost_ReturnsFailedSnapshot()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetHardwareSnapshotAsync(string.Empty);

        Assert.False(snapshot.IsSuccess);
        Assert.NotEmpty(snapshot.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public void ComputerHardwareSnapshot_FormattedBuild_ComputesProperly()
    {
        var snapWithDisplay = new ComputerHardwareSnapshot
        {
            BuildNumber = "26100",
            DisplayVersion = "24H2"
        };
        Assert.Equal("24H2 (Build 26100)", snapWithDisplay.FormattedBuild);

        var snapWithoutDisplay = new ComputerHardwareSnapshot
        {
            BuildNumber = "26100",
            DisplayVersion = ""
        };
        Assert.Equal("Build 26100", snapWithoutDisplay.FormattedBuild);

        var snapEmpty = new ComputerHardwareSnapshot();
        Assert.Equal("—", snapEmpty.FormattedBuild);
    }

    [Fact]
    public void ComputerHardwareSnapshot_HasWarrantySupport_IdentifiesSupportedVendors()
    {
        var dell = new ComputerHardwareSnapshot { Manufacturer = "Dell Inc.", SerialNumber = "ABC1234" };
        var lenovo = new ComputerHardwareSnapshot { Manufacturer = "Lenovo", SerialNumber = "XYZ5678" };
        var hp = new ComputerHardwareSnapshot { Manufacturer = "Hewlett-Packard", SerialNumber = "HP999" };
        var vmware = new ComputerHardwareSnapshot { Manufacturer = "VMware, Inc.", SerialNumber = "VMware-123" };
        var noSerial = new ComputerHardwareSnapshot { Manufacturer = "Dell Inc.", SerialNumber = "" };

        Assert.True(dell.HasWarrantySupport);
        Assert.True(lenovo.HasWarrantySupport);
        Assert.True(hp.HasWarrantySupport);
        Assert.False(vmware.HasWarrantySupport);
        Assert.False(noSerial.HasWarrantySupport);
    }

    [Fact]
    public void ComputerWorkspaceViewModel_HardwareState_CalculatesReactiveProperties()
    {
        var mockAd = new MockAdService();
        var mockNav = new NavigationService();
        var mockDiag = new ComputerDiagnosticService();
        var vm = new ComputerWorkspaceViewModel(mockAd, mockNav, mockDiag);

        Assert.Null(vm.HardwareSnapshot);
        Assert.False(vm.HasHardwareSnapshot);
        Assert.False(vm.HasHardwareError);
        Assert.False(vm.HasWarrantyLink);

        vm.HardwareSnapshot = new ComputerHardwareSnapshot
        {
            Manufacturer = "Dell Inc.",
            Model = "Latitude 5540",
            SerialNumber = "7G8X9Y2",
            BiosVersion = "1.14.0",
            BuildNumber = "26100",
            DisplayVersion = "24H2",
            IsSuccess = true
        };

        Assert.True(vm.HasHardwareSnapshot);
        Assert.False(vm.HasHardwareError);
        Assert.True(vm.HasWarrantyLink);
        Assert.Contains("7G8X9Y2", vm.WarrantyUrl);

        vm.ResetToHeroState();
        Assert.Null(vm.HardwareSnapshot);
        Assert.False(vm.HasHardwareSnapshot);
        Assert.False(vm.IsHardwareLoading);
    }

    [Fact]
    public void ComputerWorkspaceViewModel_GroupFilteringAndBadges_WorkAccurately()
    {
        var mockAd = new MockAdService();
        var mockNav = new NavigationService();
        var mockDiag = new ComputerDiagnosticService();
        var vm = new ComputerWorkspaceViewModel(mockAd, mockNav, mockDiag);

        var comp = new AdComputer
        {
            Name = "TEST-PC",
            Groups = new List<string> { "Domain Computers", "Workstations-All", "VPN-Users" }
        };

        vm.CurrentComputer = comp;
        vm.RefreshFilteredGroups();
        vm.NotifyPropertiesChanged();

        Assert.Equal("3", vm.GroupCountBadge);
        Assert.Equal(3, vm.FilteredGroups.Count);
        Assert.False(vm.HasNoFilteredGroups);

        vm.FilterGroups("VPN");
        Assert.Single(vm.FilteredGroups);
        Assert.Equal("VPN-Users", vm.FilteredGroups[0]);
        Assert.False(vm.HasNoFilteredGroups);

        vm.FilterGroups("NonExistentGroup");
        Assert.Empty(vm.FilteredGroups);
        Assert.True(vm.HasNoFilteredGroups);
    }

    [Fact]
    public void ComputerWorkspaceViewModel_CreatedAndModified_FormatCorrectly()
    {
        var mockAd = new MockAdService();
        var mockNav = new NavigationService();
        var mockDiag = new ComputerDiagnosticService();
        var vm = new ComputerWorkspaceViewModel(mockAd, mockNav, mockDiag);
        var testDateCreated = new DateTime(2025, 3, 15, 10, 30, 0);
        var testDateModified = new DateTime(2025, 4, 20, 14, 45, 0);

        vm.CurrentComputer = new AdComputer
        {
            Name = "TEST-PC",
            SamAccountName = "TEST-PC$",
            Created = testDateCreated,
            Modified = testDateModified
        };

        Assert.Equal(testDateCreated.ToString("g"), vm.FormattedCreated);
        Assert.Equal(testDateModified.ToString("g"), vm.FormattedModified);
    }

    [Fact]
    public void UserWorkspaceViewModel_CreatedAndModified_FormatCorrectly()
    {
        var mockAd = new MockAdService();
        var mockSearchSvc = new MockSearchService();
        var mockSearch = new GlobalSearchViewModel(mockSearchSvc);
        var mockGreeting = new GreetingService();
        var vm = new UserWorkspaceViewModel(mockAd, mockSearch, mockGreeting);
        var testDateCreated = new DateTime(2024, 1, 10, 8, 15, 0);
        var testDateModified = new DateTime(2024, 6, 25, 16, 50, 0);

        vm.CurrentUser = new AdUser
        {
            DisplayName = "Test User",
            SamAccountName = "test.user",
            Created = testDateCreated,
            Modified = testDateModified
        };

        Assert.Equal(testDateCreated.ToString("g"), vm.FormattedCreated);
        Assert.Equal(testDateModified.ToString("g"), vm.FormattedModified);
    }

    [Fact]
    public void UserWorkspaceViewModel_ManagerAndDirectReports_ComputeCorrectly()
    {
        var mockAd = new MockAdService();
        var mockSearchSvc = new MockSearchService();
        var mockSearch = new GlobalSearchViewModel(mockSearchSvc);
        var mockGreeting = new GreetingService();
        var vm = new UserWorkspaceViewModel(mockAd, mockSearch, mockGreeting);

        vm.CurrentUser = new AdUser
        {
            DisplayName = "Test User",
            SamAccountName = "test.user",
            Manager = "Claudia Weber",
            DirectReports = new List<string> { "Erika Musterfrau", "Alex Schmidt" }
        };

        Assert.True(vm.HasManager);
        Assert.True(vm.HasDirectReports);
        Assert.Equal("2", vm.DirectReportsCountBadge);
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Visible, vm.ManagerDisplayVisibility);
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, vm.NoManagerDisplayVisibility);

        // When no manager
        vm.CurrentUser = vm.CurrentUser with { Manager = string.Empty, DirectReports = [] };
        Assert.False(vm.HasManager);
        Assert.False(vm.HasDirectReports);
        Assert.Equal("0", vm.DirectReportsCountBadge);
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Collapsed, vm.ManagerDisplayVisibility);
        Assert.Equal(Microsoft.UI.Xaml.Visibility.Visible, vm.NoManagerDisplayVisibility);
    }

    [Fact]
    public void ParseCimDateTime_ValidAndInvalidFormats_HandledSafely()
    {
        var valid = ComputerDiagnosticService.ParseCimDateTime("20240412153000.000000+000");
        Assert.NotNull(valid);
        Assert.Equal(2024, valid.Value.Year);
        Assert.Equal(4, valid.Value.Month);
        Assert.Equal(12, valid.Value.Day);

        var validSimple = ComputerDiagnosticService.ParseCimDateTime("20240412153000");
        Assert.NotNull(validSimple);

        var invalid = ComputerDiagnosticService.ParseCimDateTime("notadate");
        Assert.Null(invalid);

        var empty = ComputerDiagnosticService.ParseCimDateTime(string.Empty);
        Assert.Null(empty);
    }

    [Fact]
    public void ComputerUptimeSnapshot_Formatting_CalculatesCorrectly()
    {
        var now = DateTime.Now;
        var bootTime = now.AddDays(-4).AddHours(-6);

        var snapshot = new ComputerUptimeSnapshot
        {
            Hostname = "PC-TEST-01",
            LastBootUpTime = bootTime,
            Uptime = TimeSpan.FromDays(4) + TimeSpan.FromHours(6),
            IsRebootPending = true,
            PendingRebootReasons = new List<string> { "Windows Update", "CBS" },
            IsSuccess = true
        };

        Assert.Contains("4", snapshot.FormattedUptime);
        Assert.Contains("6", snapshot.FormattedUptime);
        Assert.Equal(bootTime.ToString("g"), snapshot.FormattedLastBoot);
        Assert.Equal("Windows Update, CBS", snapshot.FormattedRebootReasons);
        Assert.True(snapshot.IsRebootPending);
    }

    [Fact]
    public async Task GetUptimeSnapshotAsync_FallbackComputers_ReturnsExpectedValues()
    {
        var service = new ComputerDiagnosticService();

        var dellSnapshot = await service.GetUptimeSnapshotAsync("PC-DELL-LATITUDE");
        Assert.True(dellSnapshot.IsSuccess);
        Assert.True(dellSnapshot.IsRebootPending);
        Assert.NotEmpty(dellSnapshot.PendingRebootReasons);

        var lenovoSnapshot = await service.GetUptimeSnapshotAsync("PC-LENOVO-THINKPAD");
        Assert.True(lenovoSnapshot.IsSuccess);
        Assert.False(lenovoSnapshot.IsRebootPending);
        Assert.Empty(lenovoSnapshot.PendingRebootReasons);
    }

    [Fact]
    public void ComputerUptimeSnapshot_UnknownRebootStatus_RendersLocalizedUnknownText()
    {
        var snapshot = new ComputerUptimeSnapshot
        {
            Hostname = "PC-PROD-01",
            IsSuccess = true,
            IsRebootStatusKnown = false
        };

        Assert.Equal(Sol.Helpers.Strings.S.RebootStatusUnknown, snapshot.RebootStatusText);
    }

    [Fact]
    public void ComputerDiskDriveInfo_Calculations_ComputeCorrectly()
    {
        ulong total = 500UL * 1024 * 1024 * 1024; // 500 GB
        ulong free = 100UL * 1024 * 1024 * 1024;  // 100 GB

        var drive = new ComputerDiskDriveInfo
        {
            DeviceId = "C:",
            VolumeName = "OSDisk",
            FileSystem = "NTFS",
            TotalBytes = total,
            FreeBytes = free,
            MediaType = "NVMe SSD",
            HealthStatus = "OK"
        };

        Assert.Equal(400UL * 1024 * 1024 * 1024, drive.UsedBytes);
        Assert.Equal(80.0, drive.UsedPercentage, 1);
        Assert.Equal(20.0, drive.FreePercentage, 1);
        Assert.Equal("500.0 GB", drive.FormattedTotalSize);
        Assert.Equal("100.0 GB", drive.FormattedFreeSpace);
        Assert.Equal("400.0 GB", drive.FormattedUsedSpace);
        Assert.Equal("C: (OSDisk)", drive.DisplayTitle);
        Assert.False(drive.IsLowSpace);
        Assert.False(drive.IsCriticalSpace);
        Assert.Contains("C: (OSDisk)", drive.CopyDetailsText);
    }

    [Fact]
    public void ComputerDiskDriveInfo_LowAndCriticalSpace_TriggersCorrectly()
    {
        ulong total = 100UL * 1024 * 1024 * 1024; // 100 GB
        ulong freeLow = 10UL * 1024 * 1024 * 1024; // 10 GB (10% -> Low Space)
        ulong freeCrit = 3UL * 1024 * 1024 * 1024; // 3 GB (3% -> Critical Space)

        var lowDrive = new ComputerDiskDriveInfo
        {
            DeviceId = "C:",
            TotalBytes = total,
            FreeBytes = freeLow
        };

        var critDrive = new ComputerDiskDriveInfo
        {
            DeviceId = "C:",
            TotalBytes = total,
            FreeBytes = freeCrit
        };

        Assert.True(lowDrive.IsLowSpace);
        Assert.False(lowDrive.IsCriticalSpace);

        Assert.True(critDrive.IsLowSpace);
        Assert.True(critDrive.IsCriticalSpace);
    }

    [Fact]
    public void ComputerDiskDriveInfo_FormatBytes_HandlesTerabytes()
    {
        ulong tbBytes = 2048UL * 1024 * 1024 * 1024; // 2.0 TB
        var formatted = ComputerDiskDriveInfo.FormatBytes(tbBytes);
        Assert.Equal("2.00 TB", formatted);
    }

    [Fact]
    public async Task GetDiskSnapshotAsync_EmptyHost_ReturnsFailedSnapshot()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetDiskSnapshotAsync(string.Empty);

        Assert.False(snapshot.IsSuccess);
        Assert.NotEmpty(snapshot.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task GetDiskSnapshotAsync_DellDemo_ReturnsExpectedPartitions()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetDiskSnapshotAsync("PC-DELL-LATITUDE.company.local");

        Assert.True(snapshot.IsSuccess);
        Assert.Equal(2, snapshot.Drives.Count);
        Assert.Equal("C:", snapshot.Drives[0].DeviceId);
        Assert.Equal("D:", snapshot.Drives[1].DeviceId);
    }

    [Fact]
    public async Task GetDiskSnapshotAsync_LenovoDemo_ReturnsLowSpacePartition()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetDiskSnapshotAsync("PC-LENOVO-THINKPAD.company.local");

        Assert.True(snapshot.IsSuccess);
        Assert.Single(snapshot.Drives);
        Assert.Equal("C:", snapshot.Drives[0].DeviceId);
        Assert.True(snapshot.Drives[0].IsLowSpace);
    }

    [Theory]
    [InlineData("localhost", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData(".", true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("PC-DELL-LATITUDE.company.local", false)]
    [InlineData("PC-LENOVO-THINKPAD.company.local", false)]
    [InlineData("SERVER-01.domain.com", false)]
    public void IsLocalHost_IdentifiesLocalHostCorrectly(string host, bool expected)
    {
        bool result = ComputerDiagnosticService.IsLocalHost(host);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("PC-DELL-LATITUDE.company.local", true)]
    [InlineData("PC-LENOVO-THINKPAD.company.local", true)]
    [InlineData("DEMO-CLIENT-01", true)]
    [InlineData("TEST-CLIENT-01", true)]
    [InlineData("localhost", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData(".", false)]
    public void IsDemoFixture_IdentifiesDemoFixturesCorrectly(string host, bool expected)
    {
        bool result = ComputerDiagnosticService.IsDemoFixture(host);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ComputerDiskDriveInfo_HealthStatusFlags_EvaluateCorrectly()
    {
        var okDrive = new ComputerDiskDriveInfo { DeviceId = "C:", HealthStatus = "OK" };
        var healthyDrive = new ComputerDiskDriveInfo { DeviceId = "C:", HealthStatus = "Healthy" };
        var warnDrive = new ComputerDiskDriveInfo { DeviceId = "D:", HealthStatus = "Warning" };
        var errorDrive = new ComputerDiskDriveInfo { DeviceId = "E:", HealthStatus = "Pred Fail" };

        Assert.True(okDrive.IsHealthOk);
        Assert.False(okDrive.IsHealthWarning);
        Assert.False(okDrive.IsHealthError);

        Assert.True(healthyDrive.IsHealthOk);
        Assert.False(healthyDrive.IsHealthWarning);
        Assert.False(healthyDrive.IsHealthError);

        Assert.False(warnDrive.IsHealthOk);
        Assert.True(warnDrive.IsHealthWarning);
        Assert.False(warnDrive.IsHealthError);

        Assert.False(errorDrive.IsHealthOk);
        Assert.False(errorDrive.IsHealthWarning);
        Assert.True(errorDrive.IsHealthError);
    }

    [Fact]
    public async Task GetDiskSnapshotAsync_UnreachableHost_ReturnsFailure()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetDiskSnapshotAsync("NON-EXISTENT-HOST-99999.invalid");

        Assert.False(snapshot.IsSuccess);
        Assert.NotNull(snapshot.ErrorMessage);
    }

    private class MockSearchService : ISearchService
    {
        public Task<IEnumerable<AdUser>> SearchUsersAsync(string query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<AdUser>>(Array.Empty<AdUser>());
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
        public Task UpdateUserProfileAsync(string samAccountName, System.Collections.Generic.Dictionary<string, string> attributes, string? newManager) => Task.CompletedTask;
        public Task UpdateRawAttributeAsync(string samAccountName, string attributeName, string newValue) => Task.CompletedTask;
        public Task AddUserToGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
        public Task RemoveUserFromGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
        public Task<List<AdComputer>> SearchComputersAsync(string query) => Task.FromResult(new List<AdComputer>());
        public Task EnableComputerAccountAsync(string samAccountName, bool enable) => Task.CompletedTask;
        public Task AddComputerToGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
        public Task RemoveComputerFromGroupAsync(string samAccountName, string groupName) => Task.CompletedTask;
    }
}
