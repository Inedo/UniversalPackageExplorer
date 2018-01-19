using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace UniversalPackageExplorer.ValidationRules
{
    public sealed class IconValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var expr = (BindingExpression)value;
            var text = (string)expr.ResolvedSource?.GetType().GetProperty(expr.ResolvedSourcePropertyName).GetValue(expr.ResolvedSource);
            if (string.IsNullOrEmpty(text))
            {
                return new ValidationResult(true, null);
            }

            if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
            {
                return new ValidationResult(false, "Invalid URI.");
            }

            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                return new ValidationResult(true, null);
            }

            if (uri.Scheme == "package")
            {
                if (((MainWindow)Application.Current.MainWindow).Package.Files.ContainsKey(uri.GetLeftPart(UriPartial.Authority)))
                {
                    return new ValidationResult(true, null);
                }
                return new ValidationResult(false, "File not found in package.");
            }

            return new ValidationResult(false, "Icon URI must start with http://, https://, or package://.");
        }
    }
}
