using Newtonsoft.Json;
using Semver;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace UniversalPackageExplorer.UPack
{
    [JsonConverter(typeof(Converter))]
    public sealed class UniversalPackageDependency : INotifyPropertyChanged, IEquatable<UniversalPackageDependency>
    {
        private static readonly Regex GroupAndNameRegex = new Regex(@"^([0-9a-zA-Z\-._]([0-9a-zA-Z\-._/]{0,48}[0-9a-zA-Z\-._])?/)?[0-9a-zA-Z\-._]{1,50}$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public UniversalPackageDependency()
        {
            this.RawValue = string.Empty;
        }

        public UniversalPackageDependency(string raw)
        {
            this.RawValue = raw.Trim('\r');
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string rawValue;
        public string RawValue
        {
            get => this.rawValue;
            set
            {
                this.rawValue = value ?? string.Empty;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEmpty)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RawValue)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationError)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupAndName)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Version)));
            }
        }
        public bool IsEmpty => string.IsNullOrEmpty(this.RawValue);
        public string ValidationError
        {
            get
            {
                var parts = this.rawValue.Split(':');

                if (parts.Length > 2)
                {
                    return "Dependency should be name:version or name.";
                }

                if (parts.Length == 2 && !SemVersion.TryParse(parts[1], out var rubbish))
                {
                    return "Invalid semantic version.";
                }

                if (!GroupAndNameRegex.IsMatch(parts[0]))
                {
                    return "Invalid package ID.";
                }

                return null;
            }
        }
        public string GroupAndName
        {
            get
            {
                if (this.IsEmpty)
                {
                    return null;
                }

                var parts = this.rawValue.Split(':');
                if (parts.Length > 2)
                {
                    return null;
                }

                if (!GroupAndNameRegex.IsMatch(parts[0]))
                {
                    return null;
                }

                return parts[0];
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public SemVersion Version
        {
            get
            {
                if (this.IsEmpty)
                {
                    return null;
                }

                var parts = this.rawValue.Split(':');
                if (parts.Length != 2)
                {
                    return null;
                }

                if (SemVersion.TryParse(parts[1], out var version))
                {
                    return version;
                }

                return null;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            return this.RawValue;
        }

        public bool Equals(UniversalPackageDependency other)
        {
            return string.Equals(this.RawValue, other?.RawValue, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this.Equals((UniversalPackageDependency)obj);
        }

        public override int GetHashCode()
        {
            return this.RawValue.GetHashCode();
        }

        private sealed class Converter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(UniversalPackageDependency);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return new UniversalPackageDependency((string)reader.Value);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((UniversalPackageDependency)value).ToString());
            }
        }
    }
}