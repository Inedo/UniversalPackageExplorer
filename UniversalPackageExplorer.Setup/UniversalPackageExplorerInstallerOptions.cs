using System;
using System.ComponentModel;
using System.IO;
using Inedo.Installer;

namespace Inedo.UniversalPackageExplorer.Setup
{
    public sealed class UniversalPackageExplorerInstallerOptions : InstallationOptionsBase
    {
        private bool acceptsEula;
        private bool userMode;
        private string targetPath;

        public bool AcceptsEula
        {
            get => this.acceptsEula;
            set => this.SetProperty(ref this.acceptsEula, value);
        }
        public override bool IsValid => true;

        public string UserModeText => this.UserMode ? "User-level installation" : "Machine-level installation";

        [InstallerArgument]
        public bool UserMode
        {
            get => this.userMode;
            set
            {
                if (this.SetProperty(ref this.userMode, value))
                {
                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(UserModeText)));
                    if (this.targetPath == null)
                        this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(TargetPath)));
                }
            }
        }
        [InstallerArgument]
        public string TargetPath
        {
            get => this.targetPath ?? this.DefaultTargetPath;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Target path is required.");
                if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    throw new ArgumentException("Target path contains invalid characters.");
                if (!Path.IsPathRooted(value))
                    throw new ArgumentException("Target path must be an absolute path.");

                var defaultPath = this.DefaultTargetPath;
                if (value == defaultPath)
                {
                    this.targetPath = null;
                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(TargetPath)));
                }
                else
                {
                    this.SetProperty(ref this.targetPath, value);
                }
            }
        }

        private string DefaultTargetPath => Path.Combine(Environment.GetFolderPath(this.UserMode ? Environment.SpecialFolder.LocalApplicationData : Environment.SpecialFolder.ProgramFiles), "Universal Package Explorer");
    }
}
