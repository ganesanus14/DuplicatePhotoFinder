using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DuplicatePhotoFinder.Converters;

/// <summary>
/// true → Visible, false → Collapsed.
/// Set <see cref="IsInverted"/> to true to flip the logic.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public object Convert(object value, Type targetType,
                          object parameter, CultureInfo culture)
    {
        bool flag = value is true;
        if (IsInverted) flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
