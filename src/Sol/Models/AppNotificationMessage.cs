using Microsoft.UI.Xaml.Controls;

namespace Sol.Models;

public enum AppNotificationSeverity
{
    Informational,
    Success,
    Warning,
    Error
}

public record AppNotificationMessage(string Message, AppNotificationSeverity Severity = AppNotificationSeverity.Success)
{
    public AppNotificationMessage(string message, InfoBarSeverity severity) 
        : this(message, severity switch
        {
            InfoBarSeverity.Informational => AppNotificationSeverity.Informational,
            InfoBarSeverity.Warning => AppNotificationSeverity.Warning,
            InfoBarSeverity.Error => AppNotificationSeverity.Error,
            _ => AppNotificationSeverity.Success
        })
    {
    }
}

public record JiraSettingsChangedMessage(bool IsEnabled);

public record ToolsSettingsChangedMessage(bool IsMmcLookupEnabled, bool IsAdminCommandsEnabled, bool IsShortcutGuideEnabled, bool IsGrabFrameEnabled = true, bool IsEditTextWindowEnabled = true);
