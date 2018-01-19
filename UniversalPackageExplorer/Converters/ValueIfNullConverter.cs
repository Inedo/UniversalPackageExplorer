using System;
using System.Globalization;
using System.Windows.Data;

namespace UniversalPackageExplorer.Converters
{
    [ValueConversion(typeof(object), typeof(object), ParameterType = typeof(object))]
    public sealed class ValueIfNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return parameter;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
