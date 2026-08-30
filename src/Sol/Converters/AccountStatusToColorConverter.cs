using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;

namespace Sol.Converters;

public class AccountStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string status)
        {
            if (status.Equals("Locked out", StringComparison.OrdinalIgnoreCase) || status.Equals("Gesperrt", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Colors.Red);
            }
            if (status.Equals("Disabled", StringComparison.OrdinalIgnoreCase) || status.Equals("Deaktiviert", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Colors.Orange);
            }
            if (status.Equals("Enabled", StringComparison.OrdinalIgnoreCase) || status.Equals("Aktiviert", StringComparison.OrdinalIgnoreCase) || status.Equals("Active", StringComparison.OrdinalIgnoreCase) || status.Equals("Aktiv", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Colors.Green);
            }
        }
        
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
