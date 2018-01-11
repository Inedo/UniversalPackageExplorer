using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UniversalPackageExplorer
{
    [ValueConversion(typeof(object), typeof(FontStyle))]
    public sealed class ItalicIfNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? FontStyles.Italic : FontStyles.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
