using System.Text;
using System.Text.Json;

namespace Sol.Services;

/// <summary>
/// Manages application settings.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    public string AdDomain { get; set; } = string.Empty;
    public string AppLanguage { get; set; } = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("de", StringComparison.OrdinalIgnoreCase) ? "de" : "en";

    public SettingsService()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sol");
        Directory.CreateDirectory(appDataDir);
        _settingsPath = Path.Combine(appDataDir, "appsettings.json");
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_settingsPath)) return;
        try
        {
            var json = File.ReadAllText(_settingsPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("AdDomain", out var adDomain))
                AdDomain = adDomain.GetString() ?? string.Empty;
            if (root.TryGetProperty("AppLanguage", out var appLang))
                AppLanguage = appLang.GetString() ?? "en";
        }
        catch { /* Settings load failure is non-fatal */ }
    }

    public void Save()
    {
        try
        {
            var obj = new Dictionary<string, object>
            {
                ["AdDomain"] = AdDomain ?? "",
                ["AppLanguage"] = AppLanguage ?? "en"
            };
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json, Encoding.UTF8);
        }
        catch { /* Settings save failure is non-fatal */ }
    }
}