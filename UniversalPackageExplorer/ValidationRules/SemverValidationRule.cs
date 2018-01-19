using Semver;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace UniversalPackageExplorer.ValidationRules
{
    public sealed class SemverValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var expr = (BindingExpression)value;
            var text = (string)expr.ResolvedSource?.GetType().GetProperty(expr.ResolvedSourcePropertyName).GetValue(expr.ResolvedSource);
            if (!string.IsNullOrEmpty(text) && SemVersion.TryParse(text, out var rubbish))
            {
                return new ValidationResult(true, null);
            }
            return new ValidationResult(false, $"'{text}' is not a valid semantic version.");
        }
    }
}
