using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class FileLocksmithServiceTests
{
    [Fact]
    public async Task FindLockingProcessesAsync_DetectsProcessHoldingFileLock()
    {
        var service = new FileLocksmithService();
        string tempFile = Path.Combine(Path.GetTempPath(), $"sol_locksmith_test_{Guid.NewGuid()}.txt");

        try
        {
            // Acquire exclusive lock on tempFile
            using (var stream = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var lockingProcesses = await service.FindLockingProcessesAsync(tempFile);

                Assert.NotEmpty(lockingProcesses);
                var currentPid = Process.GetCurrentProcess().Id;
                Assert.Contains(lockingProcesses, p => p.ProcessId == currentPid);
            }

            // After releasing lock, should report no locking processes
            var releasedProcesses = await service.FindLockingProcessesAsync(tempFile);
            Assert.Empty(releasedProcesses);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }

    [Fact]
    public async Task FindLockingProcessesAsync_HandlesEmptyOrWhitespacePathSafely()
    {
        var service = new FileLocksmithService();

        var res1 = await service.FindLockingProcessesAsync(string.Empty);
        var res2 = await service.FindLockingProcessesAsync("   ");
        var res3 = await service.FindLockingProcessesAsync(new string[] { });

        Assert.Empty(res1);
        Assert.Empty(res2);
        Assert.Empty(res3);
    }

    [Fact]
    public void ContextMenuRegistry_OperationsExecuteSafely()
    {
        var service = new FileLocksmithService();

        // Check registration status (should not throw)
        bool initialStatus = service.IsContextMenuRegistered();

        // Attempt safe removal (should return true or false, never throw)
        bool unsetResult = service.SetContextMenuRegistered(false);
        Assert.False(service.IsContextMenuRegistered());
    }
}
