using DuplicateEngine.Models;
using System.Globalization;
using System.Windows.Data;

namespace DuplicatePhotoFinder.Converters;

/// <summary>Converts DetectionMode enum values to friendly display strings.</summary>
public class DetectionModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
                          object parameter, CultureInfo culture)
    {
        return value is DetectionMode mode
            ? mode switch
            {
                DetectionMode.ExactOnly => "Exact Only (SHA-256)",
                DetectionMode.Strict => "Strict (pHash ≤ 2)",
                DetectionMode.Moderate => "Moderate (pHash ≤ 5)",
                DetectionMode.Loose => "Loose (pHash ≤ 10)",
                _ => mode.ToString()
            }
            : value;
    }

    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
