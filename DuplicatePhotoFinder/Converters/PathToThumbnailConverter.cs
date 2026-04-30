using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DuplicatePhotoFinder.Converters;

/// <summary>
/// Converts a file-system path to a memory-efficient <see cref="BitmapImage"/>
/// decoded at thumbnail height (160 px). The image is frozen for thread safety.
/// </summary>
public class PathToThumbnailConverter : IValueConverter
{
    public object? Convert(object value, Type targetType,
                           object parameter, CultureInfo culture)
    {
        if (value is not string path || !File.Exists(path))
            return null;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.DecodePixelHeight = 160;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.EndInit();
            bmp.Freeze();  // allow cross-thread access
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType,
                              object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
