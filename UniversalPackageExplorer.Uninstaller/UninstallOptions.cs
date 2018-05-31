using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace Inedo.UniversalPackageExplorer.Uninstaller
{
    internal sealed class UninstallOptions : INotifyPropertyChanged
    {
        private string targetPath;
        private bool invalid;
        private bool running;

        public event PropertyChangedEventHandler PropertyChanged;

        public static UninstallOptions Instance { get; } = ReadFromInstallation();

        public bool Invalid
        {
            get => this.invalid;
            private set
            {
                if (this.SetValue(ref this.invalid, value))
                    this.OnPropertyChanged(nameof(Valid));
            }
        }
        public bool Valid => !this.Invalid;
        public bool AutodetectFailure { get; private set; }
        public bool Running
        {
            get => this.running;
            set
            {
                if (this.SetValue(ref this.running, value))
                    this.OnPropertyChanged(nameof(NotRunning));
            }
        }
        public bool NotRunning => !this.running;

        public string TargetPath
        {
            get => this.targetPath;
            set => this.SetValue(ref this.targetPath, value);
        }

        private static UninstallOptions ReadFromInstallation()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Universal Package Explorer", false))
                {
                    var uninstallerPath = (string)key.GetValue("UninstallString");
                    if (!string.IsNullOrWhiteSpace(uninstallerPath))
                    {
                        var targetPath = Path.GetDirectoryName(uninstallerPath);
                        if (!Directory.Exists(targetPath))
                            targetPath = string.Empty;
                        return new UninstallOptions
                        {
                            TargetPath = targetPath,
                            AutodetectFailure = string.IsNullOrEmpty(targetPath)
                        };
                    }
                }
            }
            catch
            {
            }

            return new UninstallOptions
            {
                Invalid = true,
                AutodetectFailure = true
            };
        }

        private bool SetValue<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                this.OnPropertyChanged(name);
                return true;
            }

            return false;
        }

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name == nameof(TargetPath))
                this.Invalid = string.IsNullOrWhiteSpace(this.TargetPath);
        }
    }
}
