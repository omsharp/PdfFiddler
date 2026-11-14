using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

public class ByteArrayToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
            return null;

        var image = new BitmapImage();
        using var ms = new MemoryStream(bytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad; // important: load now, not lazily
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze(); // allows cross-thread use, better perf
        return image;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}