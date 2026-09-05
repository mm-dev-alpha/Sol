using System.Collections.Generic;

namespace Sol.Models;

public record LockingProcessInfo(
    int ProcessId,
    string ProcessName,
    string ApplicationPath,
    string User,
    IReadOnlyList<string> LockedPaths);
