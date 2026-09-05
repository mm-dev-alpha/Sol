using Microsoft.UI.Xaml.Controls;

namespace Sol.Models;

public record AppNotificationMessage(string Message, InfoBarSeverity Severity = InfoBarSeverity.Success);

public record JiraSettingsChangedMessage(bool IsEnabled);

public record ToolsSettingsChangedMessage(bool IsMmcLookupEnabled, bool IsAdminCommandsEnabled, bool IsShortcutGuideEnabled, bool IsGrabFrameEnabled = true, bool IsEditTextWindowEnabled = true);
