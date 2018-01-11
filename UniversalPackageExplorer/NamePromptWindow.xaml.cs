using System;
using System.ComponentModel;
using System.Windows;

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
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ValidationError));
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
    }
}
