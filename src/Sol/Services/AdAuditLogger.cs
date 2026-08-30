using System;
using System.IO;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sol.Services;

public record AdAuditEntry
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string OperatorIdentity { get; init; } = string.Empty;
    public string TargetUserSam { get; init; } = string.Empty;
    public string AttributeName { get; init; } = string.Empty;
    public string OldValue { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

public static class AdAuditLogger
{
    private static readonly object _syncLock = new();
    private static string? _customLogDirectory;

    public static void SetCustomLogDirectoryForTesting(string? dir)
    {
        _customLogDirectory = dir;
    }

    public static string LogDirectory
    {
        get
        {
            if (!string.IsNullOrEmpty(_customLogDirectory))
                return _customLogDirectory;

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Sol", "Logs");
        }
    }

    public static string LogFilePath => Path.Combine(LogDirectory, "ad_audit.log");

    public static async Task LogAttributeChangeAsync(string targetUserSam, string attributeName, string oldValue, string newValue, bool success, string? errorMessage = null)
    {
        var operatorName = "Unknown";
        try
        {
            operatorName = WindowsIdentity.GetCurrent().Name;
        }
        catch
        {
            // Fallback if identity cannot be retrieved
        }

        var entry = new AdAuditEntry
        {
            TimestampUtc = DateTime.UtcNow,
            OperatorIdentity = operatorName,
            TargetUserSam = targetUserSam,
            AttributeName = attributeName,
            OldValue = oldValue,
            NewValue = newValue,
            Success = success,
            ErrorMessage = errorMessage
        };

        var jsonLine = JsonSerializer.Serialize(entry);

        await Task.Run(() =>
        {
            lock (_syncLock)
            {
                Directory.CreateDirectory(LogDirectory);
                
                // Roll log if exceeds 5MB
                try
                {
                    var fileInfo = new FileInfo(LogFilePath);
                    if (fileInfo.Exists && fileInfo.Length > 5 * 1024 * 1024)
                    {
                        string oldLogPath = Path.Combine(LogDirectory, "ad_audit.old.log");
                        if (File.Exists(oldLogPath)) File.Delete(oldLogPath);
                        File.Move(LogFilePath, oldLogPath);
                    }
                }
                catch { }

                File.AppendAllText(LogFilePath, jsonLine + Environment.NewLine);
            }
        });
    }
}
