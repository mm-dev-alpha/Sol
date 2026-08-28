namespace Sol.Services;

/// <summary>
/// Manages application settings.
/// Settings are persisted to a JSON file in LocalApplicationData.
/// </summary>
public interface ISettingsService
{
    string AdDomain { get; set; }
    string AppLanguage { get; set; }

    void Load();
    void Save();
}
