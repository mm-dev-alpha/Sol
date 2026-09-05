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
    public string AppLanguage { get; set; } = "en";

    public bool IsJiraEnabled { get; set; } = false;
    public string JiraDeploymentMode { get; set; } = "DataCenter";
    public string JiraBaseUrl { get; set; } = string.Empty;
    public string JiraCloudEmail { get; set; } = string.Empty;

    // Tools Configuration Defaults
    public bool AwakeKeepDisplayOnDefault { get; set; } = false;
    public int AwakeDefaultTimeMinutes { get; set; } = 30;
    public bool IsMmcLookupEnabled { get; set; } = true;
    public bool IsAdminCommandsEnabled { get; set; } = true;
    public bool IsShortcutGuideEnabled { get; set; } = true;
    public bool IsFileLocksmithShellIntegrationEnabled { get; set; } = false;
    public bool IsGrabFrameEnabled { get; set; } = true;
    public bool GrabFrameAutoOcr { get; set; } = false;
    public bool GrabFrameAlwaysOnTop { get; set; } = true;
    public bool GrabFrameSingleLine { get; set; } = false;
    public bool GrabFrameTableMode { get; set; } = false;
    public bool GrabFrameAutoPaste { get; set; } = false;
    public string GrabFrameDefaultLanguage { get; set; } = "en-US";
    public bool IsEditTextWindowEnabled { get; set; } = true;
    public bool EditTextWindowWordWrap { get; set; } = true;
    public bool EditTextWindowAlwaysOnTop { get; set; } = false;

    public SettingsService(string? settingsPath = null)
    {
        if (!string.IsNullOrEmpty(settingsPath))
        {
            _settingsPath = settingsPath;
        }
        else
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Sol");
            Directory.CreateDirectory(appDataDir);
            _settingsPath = Path.Combine(appDataDir, "appsettings.json");
        }
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

            if (root.TryGetProperty("AwakeKeepDisplayOnDefault", out var keepDisp))
                AwakeKeepDisplayOnDefault = keepDisp.GetBoolean();
            if (root.TryGetProperty("AwakeDefaultTimeMinutes", out var awakeMins))
                AwakeDefaultTimeMinutes = awakeMins.GetInt32();
            if (root.TryGetProperty("IsMmcLookupEnabled", out var mmcEn))
                IsMmcLookupEnabled = mmcEn.GetBoolean();
            if (root.TryGetProperty("IsAdminCommandsEnabled", out var acEn))
                IsAdminCommandsEnabled = acEn.GetBoolean();
            if (root.TryGetProperty("IsShortcutGuideEnabled", out var scgEn))
                IsShortcutGuideEnabled = scgEn.GetBoolean();
            if (root.TryGetProperty("IsFileLocksmithShellIntegrationEnabled", out var flEn))
                IsFileLocksmithShellIntegrationEnabled = flEn.GetBoolean();
            if (root.TryGetProperty("IsGrabFrameEnabled", out var gfEn))
                IsGrabFrameEnabled = gfEn.GetBoolean();
            if (root.TryGetProperty("GrabFrameAutoOcr", out var gfAo))
                GrabFrameAutoOcr = gfAo.GetBoolean();
            if (root.TryGetProperty("GrabFrameAlwaysOnTop", out var gfAot))
                GrabFrameAlwaysOnTop = gfAot.GetBoolean();
            if (root.TryGetProperty("GrabFrameSingleLine", out var gfSl))
                GrabFrameSingleLine = gfSl.GetBoolean();
            if (root.TryGetProperty("GrabFrameTableMode", out var gfTab))
                GrabFrameTableMode = gfTab.GetBoolean();
            if (root.TryGetProperty("GrabFrameAutoPaste", out var gfAp))
                GrabFrameAutoPaste = gfAp.GetBoolean();
            if (root.TryGetProperty("GrabFrameDefaultLanguage", out var gfLang))
                GrabFrameDefaultLanguage = gfLang.GetString() ?? "en-US";
            if (root.TryGetProperty("IsEditTextWindowEnabled", out var etwEn))
                IsEditTextWindowEnabled = etwEn.GetBoolean();
            if (root.TryGetProperty("EditTextWindowWordWrap", out var etwWw))
                EditTextWindowWordWrap = etwWw.GetBoolean();
            if (root.TryGetProperty("EditTextWindowAlwaysOnTop", out var etwAot))
                EditTextWindowAlwaysOnTop = etwAot.GetBoolean();
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
                ["JiraCloudEmail"] = JiraCloudEmail ?? "",
                ["AwakeKeepDisplayOnDefault"] = AwakeKeepDisplayOnDefault,
                ["AwakeDefaultTimeMinutes"] = AwakeDefaultTimeMinutes,
                ["IsMmcLookupEnabled"] = IsMmcLookupEnabled,
                ["IsAdminCommandsEnabled"] = IsAdminCommandsEnabled,
                ["IsShortcutGuideEnabled"] = IsShortcutGuideEnabled,
                ["IsFileLocksmithShellIntegrationEnabled"] = IsFileLocksmithShellIntegrationEnabled,
                ["IsGrabFrameEnabled"] = IsGrabFrameEnabled,
                ["GrabFrameAutoOcr"] = GrabFrameAutoOcr,
                ["GrabFrameAlwaysOnTop"] = GrabFrameAlwaysOnTop,
                ["GrabFrameSingleLine"] = GrabFrameSingleLine,
                ["GrabFrameTableMode"] = GrabFrameTableMode,
                ["GrabFrameAutoPaste"] = GrabFrameAutoPaste,
                ["GrabFrameDefaultLanguage"] = GrabFrameDefaultLanguage ?? "en-US",
                ["IsEditTextWindowEnabled"] = IsEditTextWindowEnabled,
                ["EditTextWindowWordWrap"] = EditTextWindowWordWrap,
                ["EditTextWindowAlwaysOnTop"] = EditTextWindowAlwaysOnTop
            };
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json, Encoding.UTF8);
        }
        catch { /* Settings save failure is non-fatal */ }
    }
}