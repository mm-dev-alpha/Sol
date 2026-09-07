namespace Sol.Services;

/// <summary>
/// Manages application settings.
/// Settings are persisted to a JSON file in LocalApplicationData.
/// </summary>
public interface ISettingsService
{
    int SchemaVersion { get; }
    bool IsDemoMode { get; set; }

    string AdDomain { get; set; }
    string AppLanguage { get; set; }

    bool IsJiraEnabled { get; set; }
    string JiraDeploymentMode { get; set; }
    string JiraBaseUrl { get; set; }
    string JiraCloudEmail { get; set; }

    // Tools Configuration
    bool IsMmcLookupEnabled { get; set; }
    bool IsAdminCommandsEnabled { get; set; }
    bool IsShortcutGuideEnabled { get; set; }
    bool IsFileLocksmithShellIntegrationEnabled { get; set; }
    bool IsGrabFrameEnabled { get; set; }
    bool GrabFrameAutoOcr { get; set; }
    bool GrabFrameAlwaysOnTop { get; set; }
    bool GrabFrameSingleLine { get; set; }
    bool GrabFrameTableMode { get; set; }
    bool GrabFrameAutoPaste { get; set; }
    string GrabFrameDefaultLanguage { get; set; }
    bool IsEditTextWindowEnabled { get; set; }
    bool EditTextWindowWordWrap { get; set; }
    bool EditTextWindowAlwaysOnTop { get; set; }

    void Load();
    void Save();
}
