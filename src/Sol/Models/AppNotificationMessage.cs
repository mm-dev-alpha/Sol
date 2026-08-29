using Microsoft.UI.Xaml.Controls;

namespace Sol.Models;

public record AppNotificationMessage(string Message, InfoBarSeverity Severity = InfoBarSeverity.Success);
