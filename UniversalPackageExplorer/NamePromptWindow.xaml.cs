using System;
using System.ComponentModel;
using System.Windows;
using UniversalPackageExplorer.UPack;

namespace UniversalPackageExplorer
{
    /// <summary>
    /// Interaction logic for NamePromptWindow.xaml
    /// </summary>
    public partial class NamePromptWindow : Window, INotifyPropertyChanged
    {
        public NamePromptWindow(string commandName, string prompt, Func<string, string> validate)
        {
            this.CommandName = commandName;
            this.Prompt = prompt;
            this.validate = validate;

            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string CommandName { get; }
        public string Prompt { get; }
        private readonly Func<string, string> validate;

        private string text;
        public string Text
        {
            get => this.text;
            set
            {
                if (this.text != value)
                {
                    this.text = value;
                    this.ValidationError = this.validate(value);
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
                }
            }
        }

        private string validationError;
        public string ValidationError
        {
            get => this.validationError;
            private set
            {
                if (this.validationError != value)
                {
                    this.validationError = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationError)));
                }
            }
        }

        private void Button_Submit(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public static Func<string, string> CreateNameValidator(bool isDirectory, UniversalPackage.FileCollection collection, bool isMetadata, string prefix, string existingName = null)
        {
            return validate;

            string validate(string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return "Enter a name.";
                }

                if (newName.Contains("/") || newName.Contains("\\"))
                {
                    return isDirectory ? "Folder names cannot contain slashes." : "File names cannot contain slashes.";
                }

                if (prefix == string.Empty && string.Equals(newName, "package", StringComparison.OrdinalIgnoreCase) && isMetadata)
                {
                    return isDirectory ? "A metadata folder cannot be named \"package\"." : "A metadata file cannot be named \"package\".";
                }

                if (string.Equals(newName, existingName, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var fullName = prefix + newName;

                if (collection.ContainsKey(fullName))
                {
                    return "Name already in use!";
                }

                try
                {
                    collection.CheckPath(fullName);
                    return null;
                }
                catch (ArgumentException ex)
                {
                    return ex.Message;
                }
            };
        }
    }
}
