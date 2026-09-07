using System.Text;
using System.Text.Json;

namespace Sol.Services;

/// <summary>
/// Manages application settings.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly object _syncLock = new();

    private int _schemaVersion = 1;
    private bool _isDemoMode = false;
    private string _adDomain = string.Empty;
    private string _appLanguage = "en";

    private bool _isJiraEnabled = false;
    private string _jiraDeploymentMode = "DataCenter";
    private string _jiraBaseUrl = string.Empty;
    private string _jiraCloudEmail = string.Empty;

    // Tools Configuration Defaults
    private bool _isMmcLookupEnabled = true;
    private bool _isAdminCommandsEnabled = true;
    private bool _isShortcutGuideEnabled = true;
    private bool _isFileLocksmithShellIntegrationEnabled = false;
    private bool _isGrabFrameEnabled = true;
    private bool _grabFrameAutoOcr = false;
    private bool _grabFrameAlwaysOnTop = true;
    private bool _grabFrameSingleLine = false;
    private bool _grabFrameTableMode = false;
    private bool _grabFrameAutoPaste = false;
    private string _grabFrameDefaultLanguage = "en-US";
    private bool _isEditTextWindowEnabled = true;
    private bool _editTextWindowWordWrap = true;
    private bool _editTextWindowAlwaysOnTop = false;

    public int SchemaVersion
    {
        get { lock (_syncLock) return _schemaVersion; }
        private set { lock (_syncLock) _schemaVersion = value; }
    }

    public bool IsDemoMode
    {
        get { lock (_syncLock) return _isDemoMode; }
        set { lock (_syncLock) _isDemoMode = value; }
    }

    public string AdDomain
    {
        get { lock (_syncLock) return _adDomain; }
        set { lock (_syncLock) _adDomain = value; }
    }

    public string AppLanguage
    {
        get { lock (_syncLock) return _appLanguage; }
        set { lock (_syncLock) _appLanguage = value; }
    }

    public bool IsJiraEnabled
    {
        get { lock (_syncLock) return _isJiraEnabled; }
        set { lock (_syncLock) _isJiraEnabled = value; }
    }

    public string JiraDeploymentMode
    {
        get { lock (_syncLock) return _jiraDeploymentMode; }
        set { lock (_syncLock) _jiraDeploymentMode = value; }
    }

    public string JiraBaseUrl
    {
        get { lock (_syncLock) return _jiraBaseUrl; }
        set { lock (_syncLock) _jiraBaseUrl = value; }
    }

    public string JiraCloudEmail
    {
        get { lock (_syncLock) return _jiraCloudEmail; }
        set { lock (_syncLock) _jiraCloudEmail = value; }
    }

    public bool IsMmcLookupEnabled
    {
        get { lock (_syncLock) return _isMmcLookupEnabled; }
        set { lock (_syncLock) _isMmcLookupEnabled = value; }
    }

    public bool IsAdminCommandsEnabled
    {
        get { lock (_syncLock) return _isAdminCommandsEnabled; }
        set { lock (_syncLock) _isAdminCommandsEnabled = value; }
    }

    public bool IsShortcutGuideEnabled
    {
        get { lock (_syncLock) return _isShortcutGuideEnabled; }
        set { lock (_syncLock) _isShortcutGuideEnabled = value; }
    }

    public bool IsFileLocksmithShellIntegrationEnabled
    {
        get { lock (_syncLock) return _isFileLocksmithShellIntegrationEnabled; }
        set { lock (_syncLock) _isFileLocksmithShellIntegrationEnabled = value; }
    }

    public bool IsGrabFrameEnabled
    {
        get { lock (_syncLock) return _isGrabFrameEnabled; }
        set { lock (_syncLock) _isGrabFrameEnabled = value; }
    }

    public bool GrabFrameAutoOcr
    {
        get { lock (_syncLock) return _grabFrameAutoOcr; }
        set { lock (_syncLock) _grabFrameAutoOcr = value; }
    }

    public bool GrabFrameAlwaysOnTop
    {
        get { lock (_syncLock) return _grabFrameAlwaysOnTop; }
        set { lock (_syncLock) _grabFrameAlwaysOnTop = value; }
    }

    public bool GrabFrameSingleLine
    {
        get { lock (_syncLock) return _grabFrameSingleLine; }
        set { lock (_syncLock) _grabFrameSingleLine = value; }
    }

    public bool GrabFrameTableMode
    {
        get { lock (_syncLock) return _grabFrameTableMode; }
        set { lock (_syncLock) _grabFrameTableMode = value; }
    }

    public bool GrabFrameAutoPaste
    {
        get { lock (_syncLock) return _grabFrameAutoPaste; }
        set { lock (_syncLock) _grabFrameAutoPaste = value; }
    }

    public string GrabFrameDefaultLanguage
    {
        get { lock (_syncLock) return _grabFrameDefaultLanguage; }
        set { lock (_syncLock) _grabFrameDefaultLanguage = value; }
    }

    public bool IsEditTextWindowEnabled
    {
        get { lock (_syncLock) return _isEditTextWindowEnabled; }
        set { lock (_syncLock) _isEditTextWindowEnabled = value; }
    }

    public bool EditTextWindowWordWrap
    {
        get { lock (_syncLock) return _editTextWindowWordWrap; }
        set { lock (_syncLock) _editTextWindowWordWrap = value; }
    }

    public bool EditTextWindowAlwaysOnTop
    {
        get { lock (_syncLock) return _editTextWindowAlwaysOnTop; }
        set { lock (_syncLock) _editTextWindowAlwaysOnTop = value; }
    }

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
        lock (_syncLock)
        {
            if (!File.Exists(_settingsPath)) return;

            string json;
            try
            {
                json = File.ReadAllText(_settingsPath);
            }
            catch
            {
                return;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                try
                {
                    var backupPath = $"{_settingsPath}.corrupt.{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
                    File.Copy(_settingsPath, backupPath, overwrite: true);
                }
                catch { }
                return;
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return;

                if (TryGetIntProperty(root, "SchemaVersion", out var sv)) _schemaVersion = sv;
                if (TryGetBoolProperty(root, "IsDemoMode", out var dm)) _isDemoMode = dm;
                if (TryGetStringProperty(root, "AdDomain", out var adDomain)) _adDomain = adDomain;
                if (TryGetStringProperty(root, "AppLanguage", out var appLang)) _appLanguage = appLang;

                if (TryGetBoolProperty(root, "IsJiraEnabled", out var jiraEnabled)) _isJiraEnabled = jiraEnabled;
                if (TryGetStringProperty(root, "JiraDeploymentMode", out var jiraMode)) _jiraDeploymentMode = jiraMode;
                if (TryGetStringProperty(root, "JiraBaseUrl", out var jiraUrl)) _jiraBaseUrl = jiraUrl;
                if (TryGetStringProperty(root, "JiraCloudEmail", out var jiraEmail)) _jiraCloudEmail = jiraEmail;

                if (TryGetBoolProperty(root, "IsMmcLookupEnabled", out var mmcEn)) _isMmcLookupEnabled = mmcEn;
                if (TryGetBoolProperty(root, "IsAdminCommandsEnabled", out var acEn)) _isAdminCommandsEnabled = acEn;
                if (TryGetBoolProperty(root, "IsShortcutGuideEnabled", out var scgEn)) _isShortcutGuideEnabled = scgEn;
                if (TryGetBoolProperty(root, "IsFileLocksmithShellIntegrationEnabled", out var flEn)) _isFileLocksmithShellIntegrationEnabled = flEn;
                if (TryGetBoolProperty(root, "IsGrabFrameEnabled", out var gfEn)) _isGrabFrameEnabled = gfEn;
                if (TryGetBoolProperty(root, "GrabFrameAutoOcr", out var gfAo)) _grabFrameAutoOcr = gfAo;
                if (TryGetBoolProperty(root, "GrabFrameAlwaysOnTop", out var gfAot)) _grabFrameAlwaysOnTop = gfAot;
                if (TryGetBoolProperty(root, "GrabFrameSingleLine", out var gfSl)) _grabFrameSingleLine = gfSl;
                if (TryGetBoolProperty(root, "GrabFrameTableMode", out var gfTab)) _grabFrameTableMode = gfTab;
                if (TryGetBoolProperty(root, "GrabFrameAutoPaste", out var gfAp)) _grabFrameAutoPaste = gfAp;
                if (TryGetStringProperty(root, "GrabFrameDefaultLanguage", out var gfLang)) _grabFrameDefaultLanguage = gfLang;
                if (TryGetBoolProperty(root, "IsEditTextWindowEnabled", out var etwEn)) _isEditTextWindowEnabled = etwEn;
                if (TryGetBoolProperty(root, "EditTextWindowWordWrap", out var etwWw)) _editTextWindowWordWrap = etwWw;
                if (TryGetBoolProperty(root, "EditTextWindowAlwaysOnTop", out var etwAot)) _editTextWindowAlwaysOnTop = etwAot;
            }
        }
    }

    public void Save()
    {
        lock (_syncLock)
        {
            try
            {
                var obj = new Dictionary<string, object>
                {
                    ["SchemaVersion"] = _schemaVersion,
                    ["IsDemoMode"] = _isDemoMode,
                    ["AdDomain"] = _adDomain ?? "",
                    ["AppLanguage"] = _appLanguage ?? "en",
                    ["IsJiraEnabled"] = _isJiraEnabled,
                    ["JiraDeploymentMode"] = _jiraDeploymentMode ?? "DataCenter",
                    ["JiraBaseUrl"] = _jiraBaseUrl ?? "",
                    ["JiraCloudEmail"] = _jiraCloudEmail ?? "",
                    ["IsMmcLookupEnabled"] = _isMmcLookupEnabled,
                    ["IsAdminCommandsEnabled"] = _isAdminCommandsEnabled,
                    ["IsShortcutGuideEnabled"] = _isShortcutGuideEnabled,
                    ["IsFileLocksmithShellIntegrationEnabled"] = _isFileLocksmithShellIntegrationEnabled,
                    ["IsGrabFrameEnabled"] = _isGrabFrameEnabled,
                    ["GrabFrameAutoOcr"] = _grabFrameAutoOcr,
                    ["GrabFrameAlwaysOnTop"] = _grabFrameAlwaysOnTop,
                    ["GrabFrameSingleLine"] = _grabFrameSingleLine,
                    ["GrabFrameTableMode"] = _grabFrameTableMode,
                    ["GrabFrameAutoPaste"] = _grabFrameAutoPaste,
                    ["GrabFrameDefaultLanguage"] = _grabFrameDefaultLanguage ?? "en-US",
                    ["IsEditTextWindowEnabled"] = _isEditTextWindowEnabled,
                    ["EditTextWindowWordWrap"] = _editTextWindowWordWrap,
                    ["EditTextWindowAlwaysOnTop"] = _editTextWindowAlwaysOnTop
                };
                var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                var tempPath = _settingsPath + ".tmp";
                File.WriteAllText(tempPath, json, Encoding.UTF8);
                File.Move(tempPath, _settingsPath, overwrite: true);
            }
            catch { /* Settings save failure is non-fatal */ }
        }
    }

    private static bool TryGetStringProperty(JsonElement root, string name, out string value)
    {
        value = string.Empty;
        try
        {
            if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                value = prop.GetString() ?? string.Empty;
                return true;
            }
        }
        catch { }
        return false;
    }

    private static bool TryGetBoolProperty(JsonElement root, string name, out bool value)
    {
        value = false;
        try
        {
            if (root.TryGetProperty(name, out var prop) &&
                (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False))
            {
                value = prop.GetBoolean();
                return true;
            }
        }
        catch { }
        return false;
    }

    private static bool TryGetIntProperty(JsonElement root, string name, out int value)
    {
        value = 0;
        try
        {
            if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var parsed))
            {
                value = parsed;
                return true;
            }
        }
        catch { }
        return false;
    }
}