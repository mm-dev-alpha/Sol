using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

public interface IFileLocksmithService
{
    Task<IReadOnlyList<LockingProcessInfo>> FindLockingProcessesAsync(string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LockingProcessInfo>> FindLockingProcessesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default);
    bool KillProcess(int processId, out string? errorMessage);
    bool KillAllProcesses(IEnumerable<int> processIds, out List<string> errors);
    bool IsContextMenuRegistered();
    bool SetContextMenuRegistered(bool enable);
}
