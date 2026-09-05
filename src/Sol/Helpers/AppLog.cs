using System;
using System.IO;

namespace Sol.Helpers;

public static class AppLog
{
    private static readonly object _lock = new();
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sol",
        "diagnostic.log");

    public static void Write(string message)
    {
        try
        {
            lock (_lock)
            {
                var dir = Path.GetDirectoryName(_logPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                System.Diagnostics.Debug.WriteLine($"[AppLog] {message}");
                File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch { }
    }
}
