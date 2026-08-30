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

    public bool IsJiraEnabled { get; set; } = false;
    public string JiraDeploymentMode { get; set; } = "DataCenter";
    public string JiraBaseUrl { get; set; } = string.Empty;
    public string JiraCloudEmail { get; set; } = string.Empty;

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

            if (root.TryGetProperty("IsJiraEnabled", out var jiraEnabled))
                IsJiraEnabled = jiraEnabled.GetBoolean();
            if (root.TryGetProperty("JiraDeploymentMode", out var jiraMode))
                JiraDeploymentMode = jiraMode.GetString() ?? "DataCenter";
            if (root.TryGetProperty("JiraBaseUrl", out var jiraUrl))
                JiraBaseUrl = jiraUrl.GetString() ?? string.Empty;
            if (root.TryGetProperty("JiraCloudEmail", out var jiraEmail))
                JiraCloudEmail = jiraEmail.GetString() ?? string.Empty;
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
                ["AppLanguage"] = AppLanguage ?? "en",
                ["IsJiraEnabled"] = IsJiraEnabled,
                ["JiraDeploymentMode"] = JiraDeploymentMode ?? "DataCenter",
                ["JiraBaseUrl"] = JiraBaseUrl ?? "",
                ["JiraCloudEmail"] = JiraCloudEmail ?? ""
            };
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json, Encoding.UTF8);
        }
        catch { /* Settings save failure is non-fatal */ }
    }
}