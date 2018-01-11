using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace UniversalPackageExplorer
{
    public sealed class TextValidationRule : ValidationRule
    {
        public string Name { get; set; }
        public bool AllowEmpty { get; set; }
        public string AllowedCharacters { get; set; }
        public string DisallowStartEndCharacters { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var expr = (BindingExpression)value;
            var text = (string)expr.ResolvedSource?.GetType().GetProperty(expr.ResolvedSourcePropertyName).GetValue(expr.ResolvedSource);
            if (string.IsNullOrEmpty(text))
            {
                if (this.AllowEmpty)
                {
                    return new ValidationResult(true, null);
                }
                return new ValidationResult(false, $"{this.Name} cannot be empty.");
            }

            if (this.MaxLength.HasValue && text.Length > this.MaxLength.Value)
            {
                return new ValidationResult(false, $"{this.Name} cannot be more than {this.MaxLength.Value} characters, but {text.Length} were entered.");
            }
            if (this.MinLength.HasValue && text.Length < this.MinLength.Value)
            {
                return new ValidationResult(false, $"{this.Name} must be at least {this.MaxLength.Value} characters, but {text.Length} were entered.");
            }

            string error;
            if (this.AllowedCharacters != null)
            {
                foreach (var c in text)
                {
                    if (!this.ValidateCharacter(this.AllowedCharacters, "contain", c, out error))
                    {
                        return new ValidationResult(false, error);
                    }
                }

                var startEndCharacters = this.AllowedCharacters;
                if (this.DisallowStartEndCharacters != null)
                {
                    startEndCharacters = new String(startEndCharacters.Except(this.DisallowStartEndCharacters).ToArray());

                    if (!this.ValidateCharacter(startEndCharacters, "start with", text[0], out error))
                    {
                        return new ValidationResult(false, error);
                    }
                    if (!this.ValidateCharacter(startEndCharacters, "end with", text[text.Length - 1], out error))
                    {
                        return new ValidationResult(false, error);
                    }
                }
            }

            return new ValidationResult(true, null);
        }

        private bool ValidateCharacter(string allowed, string context, char c, out string error)
        {
            error = $"{this.Name} cannot {context} the character '{c}'.";
            if (c >= 'a' && c <= 'z')
            {
                return allowed.Contains("a");
            }
            if (c >= 'A' && c <= 'Z')
            {
                return allowed.Contains("A");
            }
            if (c >= '0' && c <= '9')
            {
                return allowed.Contains("0");
            }
            return allowed.Contains(c);
        }
    }
}
