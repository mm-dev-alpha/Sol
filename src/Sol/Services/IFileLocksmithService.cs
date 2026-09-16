using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

public interface IFileLocksmithService
{
    Task<IReadOnlyList<LockingProcessInfo>> FindLockingProcessesAsync(string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LockingProcessInfo>> FindLockingProcessesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default);
    Task<(bool Success, string? ErrorMessage)> KillProcessAsync(int processId, CancellationToken cancellationToken = default);
    Task<(bool Success, List<string> Errors)> KillAllProcessesAsync(IEnumerable<int> processIds, CancellationToken cancellationToken = default);
    bool KillProcess(int processId, out string? errorMessage);
    bool KillAllProcesses(IEnumerable<int> processIds, out List<string> errors);
    bool IsContextMenuRegistered();
    bool SetContextMenuRegistered(bool enable);
}
