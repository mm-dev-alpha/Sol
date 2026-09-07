using System;
using System.Threading.Tasks;
using Sol.Models;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class DecomposedDiagnosticServicesTests
{
    [Fact]
    public async Task HardwareDiagnosticService_EmptyHost_ReturnsErrorSnapshot()
    {
        IHardwareDiagnosticService service = new HardwareDiagnosticService();

        var uptime = await service.GetUptimeSnapshotAsync(string.Empty);
        var hw = await service.GetHardwareSnapshotAsync("   ");
        var disk = await service.GetDiskSnapshotAsync("");
        var battery = await service.GetBatterySnapshotAsync("  ");

        Assert.False(uptime.IsSuccess);
        Assert.False(hw.IsSuccess);
        Assert.False(disk.IsSuccess);
        Assert.False(battery.IsSuccess);
    }

    [Fact]
    public async Task HardwareDiagnosticService_DemoFixture_ReturnsSuccessfulSnapshots()
    {
        IHardwareDiagnosticService service = new HardwareDiagnosticService();
        string demoHost = "PC-LENOVO-THINKPAD";

        var uptime = await service.GetUptimeSnapshotAsync(demoHost);
        var hw = await service.GetHardwareSnapshotAsync(demoHost);
        var disk = await service.GetDiskSnapshotAsync(demoHost);
        var battery = await service.GetBatterySnapshotAsync(demoHost);

        Assert.True(uptime.IsSuccess);
        Assert.True(hw.IsSuccess);
        Assert.True(disk.IsSuccess);
        Assert.True(battery.IsSuccess);
        Assert.Equal(demoHost, hw.Hostname);
        Assert.NotEmpty(disk.Drives);
    }

    [Fact]
    public void HardwareDiagnosticService_WarrantyUrls_GeneratedAccurately()
    {
        IHardwareDiagnosticService service = new HardwareDiagnosticService();

        var dell = service.GetWarrantyUrl("Dell Inc.", "ABC1234");
        var lenovo = service.GetWarrantyUrl("Lenovo", "PF123456");
        var hp = service.GetWarrantyUrl("Hewlett-Packard", "5CD1234567");
        var unknown = service.GetWarrantyUrl("Generic Box", "1234");

        Assert.NotNull(dell);
        Assert.Contains("dell.com", dell);
        Assert.NotNull(lenovo);
        Assert.Contains("lenovo.com", lenovo);
        Assert.NotNull(hp);
        Assert.Contains("hp.com", hp);
        Assert.Null(unknown);
    }

    [Fact]
    public async Task ProcessManagementService_EmptyHost_ReturnsErrorSnapshot()
    {
        IProcessManagementService service = new ProcessManagementService();

        var procs = await service.GetProcessesSnapshotAsync(string.Empty);
        var sess = await service.GetSessionSnapshotAsync("  ");
        var svcs = await service.GetServicesSnapshotAsync("");

        Assert.False(procs.IsSuccess);
        Assert.False(sess.IsSuccess);
        Assert.False(svcs.IsSuccess);
    }

    [Fact]
    public async Task ProcessManagementService_DemoFixture_ProcessesAndSessionsQueryable()
    {
        IProcessManagementService service = new ProcessManagementService();
        string demoHost = "PC-LENOVO-THINKPAD";

        var procs = await service.GetProcessesSnapshotAsync(demoHost);
        var sess = await service.GetSessionSnapshotAsync(demoHost);
        var svcs = await service.GetServicesSnapshotAsync(demoHost);

        Assert.True(procs.IsSuccess);
        Assert.True(sess.IsSuccess);
        Assert.True(svcs.IsSuccess);
        Assert.NotEmpty(procs.Processes);
        Assert.NotEmpty(sess.Sessions);
        Assert.NotEmpty(svcs.Services);
    }

    [Fact]
    public async Task ProcessManagementService_DemoActions_StatePersistedInFixtures()
    {
        IProcessManagementService service = new ProcessManagementService();
        string demoHost = "PC-LENOVO-THINKPAD";

        // Terminate demo process
        bool termResult = await service.TerminateProcessAsync(demoHost, 10240);
        Assert.True(termResult);
        Assert.True(ComputerDiagnosticFixtures.TerminatedDemoProcesses.ContainsKey($"{demoHost}:10240"));

        // Service management
        bool stopResult = await service.StopServiceAsync(demoHost, "Spooler");
        Assert.True(stopResult);
        Assert.Equal("Stopped", ComputerDiagnosticFixtures.DemoServiceStates[$"{demoHost}:Spooler"]);

        bool startResult = await service.StartServiceAsync(demoHost, "Spooler");
        Assert.True(startResult);
        Assert.Equal("Running", ComputerDiagnosticFixtures.DemoServiceStates[$"{demoHost}:Spooler"]);

        bool modeResult = await service.SetServiceStartModeAsync(demoHost, "Spooler", "Disabled");
        Assert.True(modeResult);
        Assert.Equal("Disabled", ComputerDiagnosticFixtures.DemoServiceStartModes[$"{demoHost}:Spooler"]);
    }

    [Fact]
    public async Task BitLockerManagementService_DemoFixture_SuspendAndResume()
    {
        IBitLockerManagementService service = new BitLockerManagementService();
        string demoHost = "PC-LENOVO-THINKPAD";

        var initialStatus = await service.GetBitLockerStatusAsync(demoHost);
        Assert.True(initialStatus.IsSuccess);

        bool suspendResult = await service.SuspendBitLockerProtectionAsync(demoHost, 1);
        Assert.True(suspendResult);

        var suspendedStatus = await service.GetBitLockerStatusAsync(demoHost);
        Assert.True(suspendedStatus.IsSuspended);

        bool resumeResult = await service.ResumeBitLockerProtectionAsync(demoHost);
        Assert.True(resumeResult);

        var resumedStatus = await service.GetBitLockerStatusAsync(demoHost);
        Assert.False(resumedStatus.IsSuspended);
    }

    [Fact]
    public async Task ComputerDiagnosticService_Facade_DelegatesToSubservices()
    {
        var facade = new ComputerDiagnosticService();
        string demoHost = "PC-LENOVO-THINKPAD";

        var hw = await facade.GetHardwareSnapshotAsync(demoHost);
        var procs = await facade.GetProcessesSnapshotAsync(demoHost);
        var bitlocker = await facade.GetBitLockerStatusAsync(demoHost);

        Assert.True(hw.IsSuccess);
        Assert.True(procs.IsSuccess);
        Assert.True(bitlocker.IsSuccess);
    }
}
