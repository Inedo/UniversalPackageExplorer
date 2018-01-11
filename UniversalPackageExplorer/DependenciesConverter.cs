using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace UniversalPackageExplorer
{
    [ValueConversion(typeof(IReadOnlyList<UniversalPackageDependency>), typeof(string))]
    public sealed class DependenciesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = string.Join("\n", ((IReadOnlyList<UniversalPackageDependency>)value).Select(d => d.RawValue.Replace("\n", "\\n")));
            if (!string.IsNullOrEmpty(text))
            {
                text += "\n";
            }
            return text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(d => new UniversalPackageDependency(d)).ToArray();
        }
    }
}
