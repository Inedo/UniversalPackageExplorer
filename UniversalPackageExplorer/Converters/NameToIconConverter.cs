using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;

namespace UniversalPackageExplorer.Converters
{
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public sealed class NameToIconConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, ImageSource> cache = new ConcurrentDictionary<string, ImageSource>();

        public static ImageSource FolderIcon => GetIcon("folder");
        public static ImageSource FileIcon { get; } = new DrawingImage(new GeometryDrawing(Brushes.Black, new Pen(), Geometry.Parse("M433.651,97.245c0-2.954-1.279-5.608-3.315-7.438l-27.632-27.632L343.618,2.938c-1.777-1.782-4.158-2.812-6.662-2.918c-0.041-0.002-0.081-0.004-0.122-0.006C336.735,0.012,336.638,0,336.538,0H66.349c-5.522,0-10,4.477-10,10v470c0,5.523,4.478,10,10,10h357.283c5.523,0,10-4.477,10-10V97.631C433.637,97.502,433.651,97.375,433.651,97.245zM346.388,34.142 l42.143,42.143l10.932,10.959h-53.074V34.142zM76.349,470V20h250.039v77.245c0,5.523,4.478,10,10,10h77.244V470H76.349z")));

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

            if (path == "C:\\WINDOWS\\System32\\shell32.dll,0")
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
