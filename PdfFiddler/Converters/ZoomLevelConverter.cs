using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace PdfFiddler.Converters;

public class ZoomLevelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double number && double.TryParse(parameter?.ToString(), out double factor))
        {
            return number * factor;
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
