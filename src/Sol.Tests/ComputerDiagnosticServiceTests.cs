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
        var mockSettings = new MockSettingsService();
        var mockNav = new MockNavigationService();
        var vm = new UserWorkspaceViewModel(mockAd, mockSearch, mockGreeting, mockSettings, mockNav);
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
        var mockSettings = new MockSettingsService();
        var mockNav = new MockNavigationService();
        var vm = new UserWorkspaceViewModel(mockAd, mockSearch, mockGreeting, mockSettings, mockNav);

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

    [Fact]
    public void ComputerBatterySnapshot_HealthMath_CalculatesCorrectly()
    {
        var battery = new ComputerBatterySnapshot
        {
            Hostname = "PC-DELL-01",
            IsSuccess = true,
            HasBattery = true,
            DesignCapacityMWh = 54000,
            FullChargeCapacityMWh = 48060, // 48060 / 54000 = 89.0%
            EstimatedChargeRemainingPercent = 92,
            CycleCount = 184,
            EstimatedRunTime = TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(15))
        };

        Assert.Equal(89.0, battery.HealthPercentage, 1);
        Assert.Equal(11.0, battery.WearPercentage, 1);
        Assert.True(battery.IsHealthOk);
        Assert.False(battery.IsHealthWarning);
        Assert.False(battery.IsHealthCritical);
        Assert.Equal("54.0 Wh", battery.FormattedDesignCapacity);
        Assert.Equal("48.1 Wh", battery.FormattedFullChargeCapacity);
        Assert.Equal("48.1 Wh / 54.0 Wh", battery.FormattedCapacitySummary);
        Assert.Contains("184", battery.FormattedCycleCount);
        Assert.Contains("4", battery.FormattedEstimatedRunTime);
        Assert.Contains("PC-DELL-01", battery.CopyDetailsText);
    }

    [Fact]
    public void ComputerBatterySnapshot_HealthFlags_EvaluateCorrectly()
    {
        var perfectBattery = new ComputerBatterySnapshot { IsSuccess = true, HasBattery = true, DesignCapacityMWh = 50000, FullChargeCapacityMWh = 50000 }; // 100%
        var freshExceedingBattery = new ComputerBatterySnapshot { IsSuccess = true, HasBattery = true, DesignCapacityMWh = 50000, FullChargeCapacityMWh = 52000 }; // Clamped to 100%
        var warnBattery = new ComputerBatterySnapshot { IsSuccess = true, HasBattery = true, DesignCapacityMWh = 50000, FullChargeCapacityMWh = 36000 }; // 72%
        var critBattery = new ComputerBatterySnapshot { IsSuccess = true, HasBattery = true, DesignCapacityMWh = 50000, FullChargeCapacityMWh = 20000 }; // 40%
        var failedBattery = new ComputerBatterySnapshot { IsSuccess = false, HasBattery = false };

        Assert.Equal(100.0, perfectBattery.HealthPercentage);
        Assert.True(perfectBattery.IsHealthOk);
        Assert.False(perfectBattery.IsHealthWarning);

        Assert.Equal(100.0, freshExceedingBattery.HealthPercentage);
        Assert.True(freshExceedingBattery.IsHealthOk);

        Assert.Equal(72.0, warnBattery.HealthPercentage, 1);
        Assert.False(warnBattery.IsHealthOk);
        Assert.True(warnBattery.IsHealthWarning);
        Assert.False(warnBattery.IsHealthCritical);

        Assert.Equal(40.0, critBattery.HealthPercentage, 1);
        Assert.False(critBattery.IsHealthOk);
        Assert.False(critBattery.IsHealthWarning);
        Assert.True(critBattery.IsHealthCritical);

        Assert.False(failedBattery.IsHealthOk);
        Assert.False(failedBattery.IsHealthWarning);
        Assert.False(failedBattery.IsHealthCritical);
    }

    [Fact]
    public async Task GetBatterySnapshotAsync_EmptyHost_ReturnsFailedSnapshot()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetBatterySnapshotAsync(string.Empty);

        Assert.False(snapshot.IsSuccess);
        Assert.NotEmpty(snapshot.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task GetBatterySnapshotAsync_DellDemo_ReturnsExpectedBatteryData()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetBatterySnapshotAsync("PC-DELL-LATITUDE.company.local");

        Assert.True(snapshot.IsSuccess);
        Assert.True(snapshot.HasBattery);
        Assert.Equal(54000u, snapshot.DesignCapacityMWh);
        Assert.Equal(48060u, snapshot.FullChargeCapacityMWh);
        Assert.Equal(89.0, snapshot.HealthPercentage, 1);
        Assert.True(snapshot.IsHealthOk);
        Assert.Equal(184, snapshot.CycleCount);
        Assert.Equal(92u, snapshot.EstimatedChargeRemainingPercent);
        Assert.False(snapshot.IsCharging);
    }

    [Fact]
    public async Task GetBatterySnapshotAsync_LenovoDemo_ReturnsDegradedBatteryWarning()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetBatterySnapshotAsync("PC-LENOVO-THINKPAD.company.local");

        Assert.True(snapshot.IsSuccess);
        Assert.True(snapshot.HasBattery);
        Assert.Equal(57000u, snapshot.DesignCapacityMWh);
        Assert.Equal(41040u, snapshot.FullChargeCapacityMWh);
        Assert.Equal(72.0, snapshot.HealthPercentage, 1);
        Assert.True(snapshot.IsHealthWarning);
        Assert.Equal(412, snapshot.CycleCount);
        Assert.Equal(45u, snapshot.EstimatedChargeRemainingPercent);
        Assert.True(snapshot.IsCharging);
    }

    [Fact]
    public async Task GetBatterySnapshotAsync_UnreachableHost_ReturnsFailure()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetBatterySnapshotAsync("NON-EXISTENT-HOST-99999.invalid");

        Assert.False(snapshot.IsSuccess);
        Assert.NotNull(snapshot.ErrorMessage);
    }

    [Fact]
    public void ComputerSessionSnapshot_SessionProperties_EvaluateCorrectly()
    {
        var session = new ComputerSessionInfo
        {
            SessionId = 1,
            Username = "m.mustermann",
            Domain = "CORP",
            SamAccountName = "m.mustermann",
            DisplayName = "Max Mustermann",
            SessionType = ComputerSessionType.Console,
            LogonTime = DateTime.Now.AddHours(-2).AddMinutes(-30),
            IsActive = true
        };

        Assert.Equal("CORP\\m.mustermann", session.FullUsername);
        Assert.Equal("Max Mustermann", session.EffectiveDisplayName);
        Assert.True(session.IsConsole);
        Assert.False(session.IsRdp);
        Assert.False(session.IsDisconnected);
        Assert.NotEmpty(session.FormattedLogonTime);
        Assert.NotEmpty(session.FormattedDuration);
        Assert.NotEmpty(session.CopyDetailsText);

        var rdpSession = session with { SessionType = ComputerSessionType.RemoteDesktop };
        Assert.True(rdpSession.IsRdp);
        Assert.False(rdpSession.IsConsole);

        var disconnectedSession = session with { SessionType = ComputerSessionType.Disconnected };
        Assert.True(disconnectedSession.IsDisconnected);
    }

    [Fact]
    public async Task GetSessionSnapshotAsync_EmptyHost_ReturnsFailure()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetSessionSnapshotAsync(string.Empty);

        Assert.False(snapshot.IsSuccess);
        Assert.NotEmpty(snapshot.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task GetSessionSnapshotAsync_DellDemo_ReturnsConsoleUser()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetSessionSnapshotAsync("PC-DELL-LATITUDE.company.local");

        Assert.True(snapshot.IsSuccess);
        Assert.True(snapshot.HasActiveSessions);
        Assert.Single(snapshot.Sessions);
        var user = snapshot.Sessions[0];
        Assert.Equal("Max Mustermann", user.EffectiveDisplayName);
        Assert.Equal("CORP\\m.mustermann", user.FullUsername);
        Assert.True(user.IsConsole);
        Assert.Equal(1u, user.SessionId);
    }

    [Fact]
    public async Task GetSessionSnapshotAsync_LenovoDemo_ReturnsRdpAndDisconnectedUsers()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetSessionSnapshotAsync("PC-LENOVO-THINKPAD.company.local");

        Assert.True(snapshot.IsSuccess);
        Assert.True(snapshot.HasActiveSessions);
        Assert.Equal(2, snapshot.Sessions.Count);

        var rdpUser = snapshot.Sessions.FirstOrDefault(s => s.IsRdp);
        Assert.NotNull(rdpUser);
        Assert.Equal("Erika Schmidt", rdpUser.EffectiveDisplayName);
        Assert.Equal(2u, rdpUser.SessionId);

        var discUser = snapshot.Sessions.FirstOrDefault(s => s.IsDisconnected);
        Assert.NotNull(discUser);
        Assert.Equal("Alexander Becker", discUser.EffectiveDisplayName);
        Assert.Equal(3u, discUser.SessionId);
    }

    [Fact]
    public async Task GetSessionSnapshotAsync_UnreachableHost_ReturnsFailure()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetSessionSnapshotAsync("NON-EXISTENT-SESSION-HOST-99999.invalid");

        Assert.False(snapshot.IsSuccess);
        Assert.NotNull(snapshot.ErrorMessage);
    }

    [Fact]
    public async Task DisconnectSessionAsync_DellDemo_RemovesSessionSuccessfully()
    {
        var service = new ComputerDiagnosticService();
        string host = "DEMO-DISCONNECT-TEST-PC.company.local";

        // Query initial
        var initialSnapshot = await service.GetSessionSnapshotAsync(host);
        Assert.True(initialSnapshot.IsSuccess);
        Assert.NotEmpty(initialSnapshot.Sessions);

        uint sessionId = initialSnapshot.Sessions[0].SessionId ?? 1;

        // Disconnect
        await service.DisconnectSessionAsync(host, sessionId);

        // Query again
        var postDisconnectSnapshot = await service.GetSessionSnapshotAsync(host);
        Assert.True(postDisconnectSnapshot.IsSuccess);
        Assert.DoesNotContain(postDisconnectSnapshot.Sessions, s => s.SessionId == sessionId);
    }

    [Fact]
    public void ComputerProcessInfo_PropertiesAndFormatters_EvaluateCorrectly()
    {
        var testDate = new DateTime(2026, 8, 30, 8, 0, 0);
        var proc = new ComputerProcessInfo
        {
            ProcessId = 6120,
            Name = "chrome.exe",
            ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            WorkingSetBytes = 1536UL * 1024UL * 1024UL,
            Owner = "CORP\\m.mustermann",
            CreationDate = testDate
        };

        Assert.Equal("1.50 GB", proc.FormattedMemory);
        Assert.Equal(1536.0, proc.MemoryMb, 1);
        Assert.Equal("CORP\\m.mustermann", proc.DisplayOwner);
        Assert.False(proc.IsCriticalSystemProcess);
        Assert.True(proc.CanTerminate);
        Assert.Equal(testDate.ToString("g"), proc.FormattedCreationDate);
    }

    [Fact]
    public void ComputerProcessInfo_CriticalSystemProcesses_CannotBeTerminated()
    {
        var systemProc = new ComputerProcessInfo { ProcessId = 4, Name = "System" };
        Assert.True(systemProc.IsCriticalSystemProcess);
        Assert.False(systemProc.CanTerminate);

        var lsassProc = new ComputerProcessInfo { ProcessId = 844, Name = "lsass.exe" };
        Assert.True(lsassProc.IsCriticalSystemProcess);
        Assert.False(lsassProc.CanTerminate);

        var csrssProc = new ComputerProcessInfo { ProcessId = 620, Name = "csrss.exe" };
        Assert.True(csrssProc.IsCriticalSystemProcess);
        Assert.False(csrssProc.CanTerminate);

        var smssProc = new ComputerProcessInfo { ProcessId = 412, Name = "smss.exe" };
        Assert.True(smssProc.IsCriticalSystemProcess);
        Assert.False(smssProc.CanTerminate);

        var svchostSystem = new ComputerProcessInfo { ProcessId = 1120, Name = "svchost.exe", Owner = "NT AUTHORITY\\SYSTEM" };
        Assert.True(svchostSystem.IsCriticalSystemProcess);
        Assert.False(svchostSystem.CanTerminate);
    }

    [Fact]
    public async Task GetProcessesSnapshotAsync_DellDemo_ReturnsProcessesAndUserOwner()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetProcessesSnapshotAsync("PC-DELL-LATITUDE.company.local");

        Assert.True(snapshot.IsSuccess);
        Assert.NotEmpty(snapshot.Processes);
        Assert.True(snapshot.TotalProcessCount >= 10);
        Assert.NotEmpty(snapshot.FormattedTotalMemory);

        var chrome = snapshot.Processes.FirstOrDefault(p => p.Name.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(chrome);
        Assert.Equal("CORP\\m.mustermann", chrome.Owner);
        Assert.True(chrome.CanTerminate);
    }

    [Fact]
    public async Task GetProcessesSnapshotAsync_LenovoDemo_ReturnsProcessesAndUserOwner()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetProcessesSnapshotAsync("PC-LENOVO-THINKPAD.company.local");

        Assert.True(snapshot.IsSuccess);
        Assert.NotEmpty(snapshot.Processes);

        var teams = snapshot.Processes.FirstOrDefault(p => p.Name.Equals("ms-teams.exe", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(teams);
        Assert.Equal("CORP\\e.schmidt", teams.Owner);
    }

    [Fact]
    public async Task TerminateProcessAsync_Demo_RemovesProcessSuccessfully()
    {
        var service = new ComputerDiagnosticService();
        string host = "DEMO-PROC-TERMINATE-PC.company.local";

        var initialSnapshot = await service.GetProcessesSnapshotAsync(host);
        Assert.True(initialSnapshot.IsSuccess);
        Assert.NotEmpty(initialSnapshot.Processes);

        var procToKill = initialSnapshot.Processes.First(p => p.CanTerminate);
        uint pid = procToKill.ProcessId;

        bool terminated = await service.TerminateProcessAsync(host, pid);
        Assert.True(terminated);

        var postSnapshot = await service.GetProcessesSnapshotAsync(host);
        Assert.True(postSnapshot.IsSuccess);
        Assert.DoesNotContain(postSnapshot.Processes, p => p.ProcessId == pid);
    }

    [Fact]
    public void ComputerWorkspaceViewModel_ProcessFilteringAndSorting_WorksCorrectly()
    {
        var mockAd = new MockAdService();
        var mockNav = new MockNavigationService();
        var mockDiag = new ComputerDiagnosticService();
        var vm = new ComputerWorkspaceViewModel(mockAd, mockNav, mockDiag);

        vm.ProcessSnapshot = new ComputerProcessSnapshot
        {
            Hostname = "TEST-PC",
            IsSuccess = true,
            Processes = new List<ComputerProcessInfo>
            {
                new() { ProcessId = 100, Name = "alpha.exe", WorkingSetBytes = 100 * 1024 * 1024, Owner = "CORP\\userA" },
                new() { ProcessId = 200, Name = "beta.exe", WorkingSetBytes = 500 * 1024 * 1024, Owner = "CORP\\userB" },
                new() { ProcessId = 300, Name = "gamma.exe", WorkingSetBytes = 250 * 1024 * 1024, Owner = "CORP\\userA" }
            }
        };

        // Default sort (Memory Descending)
        vm.ApplyProcessFilterAndSort();
        Assert.Equal(3, vm.FilteredProcesses.Count);
        Assert.Equal("beta.exe", vm.FilteredProcesses[0].Name);
        Assert.Equal("gamma.exe", vm.FilteredProcesses[1].Name);
        Assert.Equal("alpha.exe", vm.FilteredProcesses[2].Name);

        // Sort Name Ascending
        vm.ToggleProcessSort("Name");
        Assert.Equal("alpha.exe", vm.FilteredProcesses[0].Name);
        Assert.Equal("beta.exe", vm.FilteredProcesses[1].Name);
        Assert.Equal("gamma.exe", vm.FilteredProcesses[2].Name);

        // Toggle Name Descending
        vm.ToggleProcessSort("Name");
        Assert.Equal("gamma.exe", vm.FilteredProcesses[0].Name);
        Assert.Equal("beta.exe", vm.FilteredProcesses[1].Name);
        Assert.Equal("alpha.exe", vm.FilteredProcesses[2].Name);

        // Sort PID Ascending
        vm.ToggleProcessSort("PID");
        Assert.Equal(100u, vm.FilteredProcesses[0].ProcessId);
        Assert.Equal(200u, vm.FilteredProcesses[1].ProcessId);
        Assert.Equal(300u, vm.FilteredProcesses[2].ProcessId);

        // Filter by user
        vm.FilterProcesses("userB");
        Assert.Single(vm.FilteredProcesses);
        Assert.Equal("beta.exe", vm.FilteredProcesses[0].Name);

        // Verify CloseProcessManagerRequested event
        bool closeRequested = false;
        vm.CloseProcessManagerRequested += () => closeRequested = true;
        vm.ResetToHeroState();
        Assert.True(closeRequested);
    }

    [Fact]
    public void BitLockerSnapshot_PropertiesAndFormatters_EvaluateCorrectly()
    {
        var snapshot = new ComputerBitLockerSnapshot
        {
            Hostname = "TEST-PC",
            DriveLetter = "C:",
            ProtectionStatus = 1,
            ConversionStatus = 1,
            EncryptionMethod = 7,
            IsSuspended = false,
            IsSuccess = true
        };

        Assert.True(snapshot.IsProtectionActive);
        Assert.False(snapshot.IsProtectionSuspended);
        Assert.True(snapshot.IsFullyEncrypted);
        Assert.Equal("XTS-AES 256-Bit", snapshot.FormattedEncryptionMethod);
        Assert.Equal("Fully Encrypted (100 %)", snapshot.FormattedConversionStatus);

        // Test Suspended State
        var suspendedSnapshot = snapshot with { IsSuspended = true, ProtectionStatus = 0 };
        Assert.False(suspendedSnapshot.IsProtectionActive);
        Assert.True(suspendedSnapshot.IsProtectionSuspended);

        // Test Other Encryption Methods
        Assert.Equal("XTS-AES 128-Bit", (snapshot with { EncryptionMethod = 6 }).FormattedEncryptionMethod);
        Assert.Equal("AES-CBC 256-Bit", (snapshot with { EncryptionMethod = 4 }).FormattedEncryptionMethod);
        Assert.Equal("AES-CBC 128-Bit", (snapshot with { EncryptionMethod = 3 }).FormattedEncryptionMethod);
        Assert.Equal("None", (snapshot with { EncryptionMethod = 0 }).FormattedEncryptionMethod);
    }

    [Fact]
    public async Task DiagnosticService_GroupPolicyUpdate_HandlesHostsCorrectly()
    {
        var service = new ComputerDiagnosticService();

        // Empty host
        Assert.False(await service.TriggerGroupPolicyUpdateAsync(string.Empty));
        Assert.False(await service.TriggerGroupPolicyUpdateAsync("   "));

        // Demo host
        bool demoResult = await service.TriggerGroupPolicyUpdateAsync("DEMO-WORKSTATION-01.contoso.local");
        Assert.True(demoResult);
    }

    [Fact]
    public async Task DiagnosticService_BitLockerStatusAndTransitions_WorkCorrectly()
    {
        var service = new ComputerDiagnosticService();
        string demoHost = "DEMO-BITLOCKER-PC.company.local";

        // 1. Initial State -> Protected
        var initialSnapshot = await service.GetBitLockerStatusAsync(demoHost);
        Assert.True(initialSnapshot.IsSuccess);
        Assert.True(initialSnapshot.IsProtectionActive);
        Assert.False(initialSnapshot.IsProtectionSuspended);
        Assert.Equal("C:", initialSnapshot.DriveLetter);

        // 2. Suspend Protection (1 Reboot)
        bool suspendSuccess = await service.SuspendBitLockerProtectionAsync(demoHost, 1);
        Assert.True(suspendSuccess);

        var suspendedSnapshot = await service.GetBitLockerStatusAsync(demoHost);
        Assert.True(suspendedSnapshot.IsSuccess);
        Assert.False(suspendedSnapshot.IsProtectionActive);
        Assert.True(suspendedSnapshot.IsProtectionSuspended);

        // 3. Resume Protection
        bool resumeSuccess = await service.ResumeBitLockerProtectionAsync(demoHost);
        Assert.True(resumeSuccess);

        var resumedSnapshot = await service.GetBitLockerStatusAsync(demoHost);
        Assert.True(resumedSnapshot.IsSuccess);
        Assert.True(resumedSnapshot.IsProtectionActive);
        Assert.False(resumedSnapshot.IsProtectionSuspended);
    }

    [Fact]
    public async Task ComputerWorkspaceViewModel_BitLockerAndGpupdateCommands_ExecuteCleanly()
    {
        var mockAd = new MockAdService();
        var mockNav = new MockNavigationService();
        var diagService = new ComputerDiagnosticService();
        var vm = new ComputerWorkspaceViewModel(mockAd, mockNav, diagService);

        vm.CurrentComputer = new AdComputer
        {
            Name = "DEMO-PC",
            DnsHostName = "DEMO-PC.company.local"
        };

        // Trigger GPUpdate
        await vm.TriggerRemoteGpupdateCommand.ExecuteAsync(null);

        // Refresh BitLocker
        await vm.RefreshBitLockerStatusCommand.ExecuteAsync(null);
        Assert.True(vm.HasBitLockerSnapshot);
        Assert.True(vm.IsBitLockerProtectionActive);

        // Suspend BitLocker
        await vm.SuspendBitLockerProtectionCommand.ExecuteAsync((uint)1);
        Assert.True(vm.IsBitLockerProtectionSuspended);

        // Resume BitLocker
        await vm.ResumeBitLockerProtectionCommand.ExecuteAsync(null);
        Assert.True(vm.IsBitLockerProtectionActive);
    }

    [Fact]
    public void JiraTicket_Model_EvaluatesPropertiesAndFormattersCorrectly()
    {
        var ticket = new JiraTicket
        {
            Key = "ITSM-1042",
            Summary = "VPN client fails to reconnect",
            Status = "In Progress",
            StatusCategoryKey = "indeterminate",
            Priority = "High",
            Created = new DateTime(2026, 8, 25, 14, 30, 0),
            BrowseUrl = "https://jira.corp.contoso.com/browse/ITSM-1042"
        };

        Assert.Equal("ITSM-1042", ticket.Key);
        Assert.True(ticket.IsInProgress);
        Assert.False(ticket.IsDone);
        Assert.Contains("2026", ticket.FormattedCreated);

        var doneTicket = ticket with { Status = "Closed", StatusCategoryKey = "done" };
        Assert.True(doneTicket.IsDone);
        Assert.False(doneTicket.IsInProgress);
    }

    [Fact]
    public async Task JiraService_DemoMode_ReturnsRealisticTickets()
    {
        var mockSettings = new MockSettingsService
        {
            IsJiraEnabled = true,
            JiraBaseUrl = "https://jira.corp.contoso.com",
            JiraDeploymentMode = "DataCenter"
        };
        var service = new JiraService(mockSettings);

        var tickets = await service.GetTicketsCreatedByUserAsync("john.doe@contoso.com", 0, 10);
        Assert.NotEmpty(tickets);
        Assert.True(tickets.Count <= 10);
        Assert.All(tickets, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Key));
            Assert.False(string.IsNullOrWhiteSpace(t.Summary));
            Assert.StartsWith("https://jira.corp.contoso.com/browse/", t.BrowseUrl);
        });

        // Empty user returns empty
        var emptyTickets = await service.GetTicketsCreatedByUserAsync("");
        Assert.Empty(emptyTickets);
    }

    [Fact]
    public async Task JiraService_TestConnection_DemoMode_Succeeds()
    {
        var mockSettings = new MockSettingsService
        {
            IsJiraEnabled = true,
            JiraBaseUrl = "https://jira.corp.contoso.com",
            JiraDeploymentMode = "DataCenter"
        };
        var service = new JiraService(mockSettings);

        bool success = await service.TestConnectionAsync();
        Assert.True(success);
    }

    [Fact]
    public async Task JiraWorkspaceViewModel_FilterAndPagination_WorkCorrectly()
    {
        var mockSettings = new MockSettingsService { IsJiraEnabled = true };
        var jiraService = new JiraService(mockSettings);
        var mockAd = new MockAdService();
        var mockSearchSvc = new MockSearchService();
        var mockSearch = new GlobalSearchViewModel(mockSearchSvc);
        var mockNav = new MockNavigationService();

        var vm = new JiraWorkspaceViewModel(jiraService, mockAd, mockSearch, mockNav);
        var user = new AdUser
        {
            DisplayName = "John Doe",
            SamAccountName = "john.doe",
            Email = "john.doe@contoso.com"
        };

        // 1. Load User
        await vm.LoadUserAsync(user);
        Assert.True(vm.HasUser);
        Assert.True(vm.HasTickets);
        Assert.False(vm.HasNoTickets);
        int initialCount = vm.FilteredTickets.Count;

        // 2. Client-side filtering
        vm.FilterQuery = "VPN";
        Assert.Single(vm.FilteredTickets);
        Assert.Equal("ITSM-1042", vm.FilteredTickets[0].Key);

        vm.FilterQuery = "NONEXISTENT_QUERY_12345";
        Assert.Empty(vm.FilteredTickets);
        Assert.True(vm.HasNoTickets);

        vm.FilterQuery = string.Empty;
        Assert.Equal(initialCount, vm.FilteredTickets.Count);

        // 3. Reset
        vm.ResetToSearchState();
        Assert.False(vm.HasUser);
        Assert.Empty(vm.AllTickets);
    }

    [Fact]
    public void SettingsService_JiraProperties_PersistAndReloadCorrectly()
    {
        var settings = new SettingsService();
        settings.IsJiraEnabled = true;
        settings.JiraDeploymentMode = "Cloud";
        settings.JiraBaseUrl = "https://test-company.atlassian.net";
        settings.JiraCloudEmail = "admin@test-company.com";
        settings.Save();

        var reloaded = new SettingsService();
        reloaded.Load();

        Assert.True(reloaded.IsJiraEnabled);
        Assert.Equal("Cloud", reloaded.JiraDeploymentMode);
        Assert.Equal("https://test-company.atlassian.net", reloaded.JiraBaseUrl);
        Assert.Equal("admin@test-company.com", reloaded.JiraCloudEmail);
    }

    [Fact]
    public void UserWorkspaceViewModel_NavigateToJira_ExecutesCleanly()
    {
        var mockAd = new MockAdService();
        var mockSearchSvc = new MockSearchService();
        var mockSearch = new GlobalSearchViewModel(mockSearchSvc);
        var mockGreeting = new GreetingService();
        var mockSettings = new MockSettingsService { IsJiraEnabled = true };
        var mockNav = new MockNavigationService();
        var vm = new UserWorkspaceViewModel(mockAd, mockSearch, mockGreeting, mockSettings, mockNav);

        vm.CurrentUser = new AdUser
        {
            DisplayName = "Test User",
            SamAccountName = "test.user",
            Email = "test.user@contoso.com"
        };

        Assert.True(vm.IsJiraEnabled);
        vm.NavigateToJiraCommand.Execute(null);
    }

    [Fact]
    public async Task ServicesSnapshot_DemoMode_ReturnsRealisticServices()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetServicesSnapshotAsync("PC-DEMO-01");

        Assert.True(snapshot.IsSuccess);
        Assert.NotEmpty(snapshot.Services);
        Assert.True(snapshot.TotalServiceCount >= 30);
        Assert.True(snapshot.RunningCount > 0);
        Assert.True(snapshot.StoppedCount > 0);

        // Check common expected Windows services
        Assert.Contains(snapshot.Services, s => s.Name == "Spooler" && s.DisplayName == "Print Spooler");
        Assert.Contains(snapshot.Services, s => s.Name == "wuauserv" && s.DisplayName == "Windows Update");
        Assert.Contains(snapshot.Services, s => s.Name == "RpcSs");
    }

    [Fact]
    public async Task ServicesSnapshot_DemoMode_IncludesMixOfStatesAndModes()
    {
        var service = new ComputerDiagnosticService();
        var snapshot = await service.GetServicesSnapshotAsync("PC-DEMO-02");

        Assert.True(snapshot.IsSuccess);
        var running = snapshot.Services.Where(s => s.IsRunning).ToList();
        var stopped = snapshot.Services.Where(s => s.IsStopped).ToList();
        var auto = snapshot.Services.Where(s => s.NormalizedStartMode == "Auto").ToList();
        var manual = snapshot.Services.Where(s => s.NormalizedStartMode == "Manual").ToList();
        var disabled = snapshot.Services.Where(s => s.NormalizedStartMode == "Disabled").ToList();

        Assert.NotEmpty(running);
        Assert.NotEmpty(stopped);
        Assert.NotEmpty(auto);
        Assert.NotEmpty(manual);
        Assert.NotEmpty(disabled);
    }

    [Fact]
    public void CriticalServiceNames_AreProtectedFromStopping()
    {
        Assert.True(ComputerServiceInfo.IsCritical("RpcSs"));
        Assert.True(ComputerServiceInfo.IsCritical("EventLog"));
        Assert.True(ComputerServiceInfo.IsCritical("PlugPlay"));
        Assert.True(ComputerServiceInfo.IsCritical("Winmgmt"));
        Assert.True(ComputerServiceInfo.IsCritical("Netlogon"));
        Assert.False(ComputerServiceInfo.IsCritical("Spooler"));
        Assert.False(ComputerServiceInfo.IsCritical("Fax"));

        var rpcSvc = new ComputerServiceInfo
        {
            Name = "RpcSs",
            DisplayName = "Remote Procedure Call (RPC)",
            State = "Running",
            StartMode = "Auto",
            AcceptStop = true
        };

        Assert.True(rpcSvc.IsCriticalService);
        Assert.False(rpcSvc.CanStop);
        Assert.False(rpcSvc.CanRestart);
        Assert.False(rpcSvc.CanChangeStartMode);

        var spooler = new ComputerServiceInfo
        {
            Name = "Spooler",
            DisplayName = "Print Spooler",
            State = "Running",
            StartMode = "Auto",
            AcceptStop = true
        };

        Assert.False(spooler.IsCriticalService);
        Assert.True(spooler.CanStop);
        Assert.True(spooler.CanRestart);
        Assert.True(spooler.CanChangeStartMode);
    }

    [Fact]
    public async Task StartStopRestartService_DemoMode_Succeeds()
    {
        var service = new ComputerDiagnosticService();

        // 1. Stop Spooler
        bool stopResult = await service.StopServiceAsync("PC-DEMO-03", "Spooler");
        Assert.True(stopResult);

        var snapshotAfterStop = await service.GetServicesSnapshotAsync("PC-DEMO-03");
        var stoppedSpooler = snapshotAfterStop.Services.FirstOrDefault(s => s.Name == "Spooler");
        Assert.NotNull(stoppedSpooler);
        Assert.Equal("Stopped", stoppedSpooler.State);

        // 2. Start Spooler
        bool startResult = await service.StartServiceAsync("PC-DEMO-03", "Spooler");
        Assert.True(startResult);

        var snapshotAfterStart = await service.GetServicesSnapshotAsync("PC-DEMO-03");
        var runningSpooler = snapshotAfterStart.Services.FirstOrDefault(s => s.Name == "Spooler");
        Assert.NotNull(runningSpooler);
        Assert.Equal("Running", runningSpooler.State);

        // 3. Restart Spooler
        bool restartResult = await service.RestartServiceAsync("PC-DEMO-03", "Spooler");
        Assert.True(restartResult);

        // 4. Critical service cannot be stopped
        bool criticalStop = await service.StopServiceAsync("PC-DEMO-03", "RpcSs");
        Assert.False(criticalStop);
    }

    [Fact]
    public async Task SetServiceStartMode_DemoMode_Succeeds()
    {
        var service = new ComputerDiagnosticService();

        // Change Spooler to Disabled
        bool setResult = await service.SetServiceStartModeAsync("PC-DEMO-04", "Spooler", "Disabled");
        Assert.True(setResult);

        var snapshot = await service.GetServicesSnapshotAsync("PC-DEMO-04");
        var spooler = snapshot.Services.FirstOrDefault(s => s.Name == "Spooler");
        Assert.NotNull(spooler);
        Assert.Equal("Disabled", spooler.StartMode);

        // Critical service cannot be modified
        bool criticalSet = await service.SetServiceStartModeAsync("PC-DEMO-04", "RpcSs", "Disabled");
        Assert.False(criticalSet);
    }

    [Fact]
    public async Task ServicesViewModel_FilterAndSort_WorkCorrectly()
    {
        var mockAd = new MockAdService();
        var mockNav = new MockNavigationService();
        var diagnosticService = new ComputerDiagnosticService();

        var vm = new ComputerWorkspaceViewModel(mockAd, mockNav, diagnosticService);
        var computer = new AdComputer
        {
            Name = "PC-DEMO-05",
            SamAccountName = "PC-DEMO-05$",
            DnsHostName = "pc-demo-05.contoso.local"
        };
        vm.CurrentComputer = computer;

        // Fetch services
        await vm.RefreshServicesAsync();

        Assert.NotNull(vm.ServicesSnapshot);
        Assert.True(vm.ServicesSnapshot.IsSuccess);
        Assert.NotEmpty(vm.FilteredServices);
        int totalCount = vm.FilteredServices.Count;

        // Filter by Running status tab
        vm.SetServiceStatusFilterCommand.Execute("Running");
        Assert.All(vm.FilteredServices, s => Assert.True(s.IsRunning));
        Assert.True(vm.FilteredServices.Count < totalCount);

        // Filter by Stopped status tab
        vm.SetServiceStatusFilterCommand.Execute("Stopped");
        Assert.All(vm.FilteredServices, s => Assert.True(s.IsStopped));

        // Filter by text search
        vm.SetServiceStatusFilterCommand.Execute("All");
        vm.FilterServicesCommand.Execute("Print");
        Assert.Contains(vm.FilteredServices, s => s.Name == "Spooler");
        Assert.All(vm.FilteredServices, s => Assert.Contains("Print", s.DisplayName, StringComparison.OrdinalIgnoreCase));

        // Toggle sort by Name
        vm.FilterServicesCommand.Execute("");
        vm.ToggleServiceSortCommand.Execute("Name");
        Assert.Equal("Name", vm.ServiceSortColumn);
        Assert.True(vm.ServiceSortAscending);
    }

    private class MockSettingsService : ISettingsService
    {
        public string AdDomain { get; set; } = "contoso.local";
        public string AppLanguage { get; set; } = "en";
        public bool IsJiraEnabled { get; set; } = false;
        public string JiraDeploymentMode { get; set; } = "DataCenter";
        public string JiraBaseUrl { get; set; } = "https://jira.corp.contoso.com";
        public string JiraCloudEmail { get; set; } = string.Empty;
        public void Load() { }
        public void Save() { }
    }

    private class MockNavigationService : INavigationService
    {
        public bool CanGoBack => false;
        public string? CurrentPageKey => "ComputerWorkspacePage";
        public event EventHandler<string>? Navigated { add { } remove { } }
        public void Initialize(Microsoft.UI.Xaml.Controls.Frame frame) { }
        public bool NavigateTo(string pageKey, object? parameter = null) => true;
        public bool GoBack() => true;
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
