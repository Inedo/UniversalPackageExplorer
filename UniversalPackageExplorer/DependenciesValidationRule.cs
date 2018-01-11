using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace UniversalPackageExplorer
{
    public sealed class DependenciesValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var expr = (BindingExpression)value;
            var deps = (IReadOnlyList<UniversalPackageDependency>)expr.ResolvedSource?.GetType().GetProperty(expr.ResolvedSourcePropertyName).GetValue(expr.ResolvedSource);
            if (deps == null)
            {
                return new ValidationResult(true, null);
            }

            foreach (var dep in deps)
            {
                var err = dep.ValidationError;
                if (err != null)
                {
                    return new ValidationResult(false, "'" + dep.RawValue + "' is invalid: " + err);
                }
            }

            return new ValidationResult(true, null);
        }
    }
}
