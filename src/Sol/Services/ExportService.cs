using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sol.Services;

public class ExportService : IExportService
{
    public async Task ExportToCsvAsync<T>(IEnumerable<T> records, Stream outputStream, CancellationToken cancellationToken = default)
    {
        using var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);
        var csvContent = await FormatAsCsvStringAsync(records, cancellationToken);
        await writer.WriteAsync(csvContent.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }

    public async Task ExportToJsonAsync<T>(T data, Stream outputStream, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        await JsonSerializer.SerializeAsync(outputStream, data, options, cancellationToken);
    }

    public Task<string> FormatAsCsvStringAsync<T>(IEnumerable<T> records, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var sb = new StringBuilder();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead && !p.PropertyType.IsGenericType)
                                 .ToArray();

            // Header line
            sb.AppendLine(string.Join(",", props.Select(p => $"\"{EscapeCsv(p.Name)}\"")));

            // Rows
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = props.Select(p =>
                {
                    var val = p.GetValue(record)?.ToString() ?? string.Empty;
                    return $"\"{EscapeCsv(val)}\"";
                });
                sb.AppendLine(string.Join(",", values));
            }

            return sb.ToString();
        }, cancellationToken);
    }

    public Task<string> FormatAsJsonStringAsync<T>(T data, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(data, options);
        }, cancellationToken);
    }

    private static string EscapeCsv(string input)
    {
        return input.Replace("\"", "\"\"");
    }
}
