using System;
using System.Globalization;
using System.Windows.Controls;

namespace UniversalPackageExplorer.ValidationRules
{
    public sealed class UPackEndpointValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!Uri.TryCreate((string)value, UriKind.Absolute, out var uri))
            {
                return new ValidationResult(false, "Invalid URI");
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return new ValidationResult(false, "URI must start with http: or https:");
            }

            return new ValidationResult(true, null);
        }
    }
}
