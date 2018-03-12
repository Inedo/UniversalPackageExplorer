using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UniversalPackageExplorer.UPack;

namespace UniversalPackageExplorer
{
    /// <summary>
    /// Interaction logic for ManifestEditor.xaml
    /// </summary>
    public partial class ManifestEditor : UserControl, INotifyPropertyChanged
    {
        public ManifestEditor()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty PackageProperty = DependencyProperty.Register(nameof(Package), typeof(UniversalPackage), typeof(ManifestEditor));

        public UniversalPackage Package
        {
            get => (UniversalPackage)this.GetValue(PackageProperty);
            set => this.SetValue(PackageProperty, value);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == PackageProperty)
            {
                if (e.OldValue is UniversalPackage oldPackage)
                {
                    oldPackage.Files.CollectionChanged -= FilesCollectionChanged;
                    oldPackage.Manifest.PropertyChanged -= ManifestPropertyChanged;
                }

                if (e.NewValue is UniversalPackage newPackage)
                {
                    newPackage.Files.CollectionChanged += FilesCollectionChanged;
                    newPackage.Manifest.PropertyChanged += ManifestPropertyChanged;
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconSource)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool inEditMode;
        public bool InEditMode
        {
            get => this.inEditMode;
            set
            {
                if (this.inEditMode != value)
                {
                    this.inEditMode = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InEditMode)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToggleButtonText)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModeVisibility)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EditModeVisibility)));
                }
            }
        }
        public string ToggleButtonText => this.InEditMode ? "View" : "Edit";
        public Visibility ViewModeVisibility => this.InEditMode ? Visibility.Collapsed : Visibility.Visible;
        public Visibility EditModeVisibility => this.InEditMode ? Visibility.Visible : Visibility.Collapsed;

        private UniversalPackageFile lastIconFile = null;
        public ImageSource IconSource
        {
            get
            {
                if (this.lastIconFile != null)
                {
                    this.lastIconFile.PropertyChanged -= IconPropertyChanged;
                    this.lastIconFile = null;
                }

                var uri = this.Package?.Manifest?.IconUri;
                if (uri == null)
                {
                    return null;
                }

                if (!this.Package.Manifest.IconText.StartsWith("package://", StringComparison.OrdinalIgnoreCase))
                {
                    return (ImageSource)new ImageSourceConverter().ConvertFrom(null, CultureInfo.InvariantCulture, uri);
                }

                if (!this.Package.Files.TryGetValue(this.Package.Manifest.IconText.Substring("package://".Length), out var file))
                {
                    return null;
                }

                this.lastIconFile = file;
                this.lastIconFile.PropertyChanged += IconPropertyChanged;

                using (var stream = file.OpenRead())
                {
                    return (ImageSource)new ImageSourceConverter().ConvertFrom(null, CultureInfo.InvariantCulture, stream);
                }
            }
        }

        private void ToggleEditMode(object sender, RoutedEventArgs e)
        {
            this.InEditMode = !this.InEditMode;
        }

        private void ManifestPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UniversalPackageManifest.IconText) || e.PropertyName == nameof(UniversalPackageManifest.IconUri))
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconSource)));
            }
        }

        private void FilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.lastIconFile != null && ((e.NewItems?.Contains(this.lastIconFile) ?? false) || (e.OldItems?.Contains(this.lastIconFile) ?? false)))
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconSource)));
            }
        }

        private void IconPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconSource)));
        }
    }
}
