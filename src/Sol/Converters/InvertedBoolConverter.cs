using Microsoft.UI.Xaml.Data;
using System;

namespace Sol.Converters;

public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b) return !b;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b) return !b;
        return true;
    }
}
