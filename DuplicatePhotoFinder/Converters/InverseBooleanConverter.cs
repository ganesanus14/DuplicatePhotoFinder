using System.Globalization;
using System.Windows.Data;

namespace DuplicatePhotoFinder.Converters;

/// <summary>Inverts a boolean value (true ↔ false).</summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
                          object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
