using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using UniversalPackageExplorer.UPack;

namespace UniversalPackageExplorer.Converters
{
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public sealed class NameToIconConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, ImageSource> cache = new ConcurrentDictionary<string, ImageSource>();

        public static ImageSource FolderIcon => GetIcon("folder");
        public static ImageSource FileIcon => GetIcon("unknown");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetIcon(Path.GetExtension((string)value));
        }

        public static ImageSource GetIcon(string extension)
        {
            return cache.GetOrAdd(extension, GetIconInternal);
        }

        private static ImageSource GetIconInternal(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return FileIcon;
            }

            string path;

            try
            {
                path = SafeNativeMethods.AssocQueryString(SafeNativeMethods.AssocStr.DefaultIcon, extension);
            }
            catch (InvalidOperationException)
            {
                return FileIcon;
            }

            return SafeNativeMethods.ExtractAssociatedIcon(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
