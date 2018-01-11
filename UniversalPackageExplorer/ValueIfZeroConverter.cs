using System;
using System.Globalization;
using System.Windows.Data;

namespace UniversalPackageExplorer
{
    [ValueConversion(typeof(int), typeof(object), ParameterType = typeof(object))]
    public sealed class ValueIfZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((int)value == 0)
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
