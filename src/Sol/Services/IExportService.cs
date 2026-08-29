using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sol.Services;

public interface IExportService
{
    Task ExportToCsvAsync<T>(IEnumerable<T> records, Stream outputStream, CancellationToken cancellationToken = default);
    Task ExportToJsonAsync<T>(T data, Stream outputStream, CancellationToken cancellationToken = default);
    Task<string> FormatAsCsvStringAsync<T>(IEnumerable<T> records, CancellationToken cancellationToken = default);
    Task<string> FormatAsJsonStringAsync<T>(T data, CancellationToken cancellationToken = default);
}
