using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace HGEngineGUI.Converters
{
    public sealed class SafeImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                var path = value as string;
                if (string.IsNullOrWhiteSpace(path)) return null;
                // Normalize to file URI if this is a filesystem path
                if (!path.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(path)) return null;
                    var uri = new Uri($"file:///{path.Replace("\\", "/")}");
                    return new BitmapImage(uri);
                }
                else
                {
                    return new BitmapImage(new Uri(path));
                }
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}


