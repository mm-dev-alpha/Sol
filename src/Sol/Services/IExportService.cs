using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

public interface IExportService
{
    Task ExportToCsvAsync<T>(IEnumerable<T> records, Stream outputStream, CancellationToken cancellationToken = default);
    Task ExportToJsonAsync<T>(T data, Stream outputStream, CancellationToken cancellationToken = default);
    Task<string> FormatAsCsvStringAsync<T>(IEnumerable<T> records, CancellationToken cancellationToken = default);
    Task<string> FormatAsJsonStringAsync<T>(T data, CancellationToken cancellationToken = default);

    string FormatUserProfileReport(AdUser user);

    string FormatComputerProfileReport(
        AdComputer computer,
        ComputerHardwareSnapshot? hardware = null,
        ComputerUptimeSnapshot? uptime = null,
        ComputerDiskSnapshot? disk = null,
        ComputerBatterySnapshot? battery = null,
        ComputerSessionSnapshot? sessions = null,
        ComputerBitLockerSnapshot? bitlocker = null,
        string? warrantyUrl = null);
}

