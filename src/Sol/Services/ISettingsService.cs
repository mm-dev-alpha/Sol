namespace Sol.Services;

/// <summary>
/// Manages application settings.
/// Settings are persisted to a JSON file in LocalApplicationData.
/// </summary>
public interface ISettingsService
{
    string AdDomain { get; set; }
    string AppLanguage { get; set; }

    bool IsJiraEnabled { get; set; }
    string JiraDeploymentMode { get; set; }
    string JiraBaseUrl { get; set; }
    string JiraCloudEmail { get; set; }

    void Load();
    void Save();
}
