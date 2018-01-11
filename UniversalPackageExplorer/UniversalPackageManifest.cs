using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Semver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace UniversalPackageExplorer
{
    [JsonObject]
    public sealed class UniversalPackageManifest : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string group;
        [JsonProperty("group", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Group
        {
            get => this.group;
            set
            {
                this.group = string.IsNullOrEmpty(value) ? null : value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Group)));
            }
        }

        private string name;
        [JsonProperty("name")]
        public string Name
        {
            get => this.name;
            set
            {
                this.name = value ?? string.Empty;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        private string version;
        [JsonProperty("version")]
        public string VersionText
        {
            get => this.version;
            set
            {
                this.version = value ?? string.Empty;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VersionText)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Version)));
            }
        }

        [JsonIgnore]
        public SemVersion Version
        {
            get
            {
                if (SemVersion.TryParse(this.version, out var v))
                {
                    return v;
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.VersionText = value.ToString();
            }
        }

        private string title;

        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Title
        {
            get => this.title;
            set
            {
                this.title = string.IsNullOrEmpty(value) ? null : value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        private string icon;

        [JsonProperty("icon", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string IconText
        {
            get => this.icon;
            set
            {
                this.icon = string.IsNullOrEmpty(value) ? null : value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconText)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconUri)));
            }
        }

        [JsonIgnore]
        public Uri IconUri
        {
            get
            {
                if (string.IsNullOrEmpty(this.icon))
                {
                    return null;
                }

                if (!Uri.TryCreate(this.icon, UriKind.Absolute, out var uri))
                {
                    return null;
                }

                if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(uri.Scheme, "package", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return uri;
            }
            set
            {
                if (value == null)
                {
                    this.IconText = null;
                }

                if (!string.Equals(value.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value.Scheme, "package", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Illegal scheme for icon URI: " + value.Scheme, nameof(value));
                }

                this.IconText = value.OriginalString;
            }
        }

        private string description;

        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Description
        {
            get => this.description;
            set
            {
                this.description = string.IsNullOrEmpty(value) ? null : value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
            }
        }

        private IReadOnlyList<UniversalPackageDependency> dependencies = new UniversalPackageDependency[0];

        [JsonProperty("dependencies")]
        public IReadOnlyList<UniversalPackageDependency> Dependencies
        {
            get => this.dependencies;
            set
            {
                this.dependencies = value?.Where(d => !d.IsEmpty).ToArray() ?? new UniversalPackageDependency[0];
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dependencies)));
            }
        }

        public bool ShouldSerializeDependencies()
        {
            return this.Dependencies.Count != 0;
        }

        [JsonExtensionData]
        public ObservableDictionary<string, JToken> UnknownFields { get; } = new ObservableDictionary<string, JToken>();
    }
}