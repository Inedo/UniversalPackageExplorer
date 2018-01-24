using Newtonsoft.Json;
using System;
using System.Linq;

namespace UniversalPackageExplorer.UPack
{
    public sealed class UniversalPackageInfo
    {
        [JsonProperty("group")]
        public string Group { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonIgnore]
        public string GroupAndName => string.IsNullOrEmpty(this.Group) ? this.Name : $"{this.Group}/{this.Name}";

        [JsonProperty("versions")]
        public string[] Versions { get; set; }

        [JsonProperty("latestVersion")]
        public string LatestVersion { get; set; }

        private string version;
        [JsonIgnore]
        public string Version
        {
            get => this.version ?? this.LatestVersion;
            set
            {
                if (!this.Versions.Contains(value))
                {
                    throw new ArgumentException("Not a version of this package", nameof(value));
                }
                this.version = value;
            }
        }

        [JsonIgnore]
        public Semver.SemVersion SemVersion => Semver.SemVersion.Parse(this.Version);

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
