using System.Globalization;
using System.Windows.Data;

namespace DuplicatePhotoFinder.Converters;

/// <summary>Converts a byte count (long) to a human-readable string (KB / MB / GB).</summary>
public class FileSizeConverter : IValueConverter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    public object Convert(object value, Type targetType,
                          object parameter, CultureInfo culture)
    {
        if (value is not long bytes) return "—";

        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < Units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {Units[unit]}";
    }

    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
