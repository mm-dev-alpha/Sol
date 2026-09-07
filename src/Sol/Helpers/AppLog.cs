using System;
using System.IO;

namespace Sol.Helpers;

public static class AppLog
{
    private static readonly object _lock = new();
    public const long MaxLogSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string _defaultLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sol",
        "diagnostic.log");

    private static string? _customLogPath;

    public static void SetLogPathForTesting(string? path)
    {
        lock (_lock)
        {
            _customLogPath = path;
        }
    }

    public static string CurrentLogPath
    {
        get
        {
            lock (_lock)
            {
                return _customLogPath ?? _defaultLogPath;
            }
        }
    }

    public static void Write(string message)
    {
        try
        {
            lock (_lock)
            {
                string path = _customLogPath ?? _defaultLogPath;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Roll log if exceeds MaxLogSizeBytes
                try
                {
                    var fileInfo = new FileInfo(path);
                    if (fileInfo.Exists && fileInfo.Length > MaxLogSizeBytes)
                    {
                        string oldLogPath = Path.Combine(
                            dir ?? string.Empty,
                            Path.GetFileNameWithoutExtension(path) + ".old" + Path.GetExtension(path));

                        if (File.Exists(oldLogPath))
                        {
                            File.Delete(oldLogPath);
                        }
                        File.Move(path, oldLogPath);
                    }
                }
                catch { }

                System.Diagnostics.Debug.WriteLine($"[AppLog] {message}");
                File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch { }
    }
}
