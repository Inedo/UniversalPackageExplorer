using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;

namespace UniversalPackageExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            this.InitializeComponent();

            var jumpList = JumpList.GetJumpList(Application.Current);
            jumpList.JumpItemsRejected += JumpList_JumpItemsRejected;
            jumpList.JumpItemsRemovedByUser += JumpList_JumpItemsRemovedByUser;
            jumpList.Apply();

            if (App.StartupPackage != null)
            {
                this.OpenFile(App.StartupPackage);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private UniversalPackage package;
        public UniversalPackage Package
        {
            get => this.package;
            internal set
            {
                if (!ReferenceEquals(this.package, value))
                {
                    this.package?.Dispose();
                }
                this.package = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Package)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPackageOpen)));
            }
        }
        public bool IsPackageOpen => this.Package != null;

        public JumpList RecentFiles => JumpList.GetJumpList(Application.Current);

        private bool operationsAllowed = true;
        public bool OperationsAllowed
        {
            get => this.operationsAllowed;
            private set
            {
                this.operationsAllowed = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OperationsAllowed)));
            }
        }

        private void OpenFile(string fullName)
        {
            this.AddRecentFile(fullName);
            this.OperationsAllowed = false;
            Task.Run(async () =>
            {
                var package = await UniversalPackage.CreateAsync(fullName);
                await this.Dispatcher.InvokeAsync(() =>
                {
                    this.Package = package;
                    this.OperationsAllowed = true;
                });
            });
        }

        private void AddRecentFile(string name)
        {
            var task = new JumpTask
            {
                ApplicationPath = Assembly.GetEntryAssembly().Location,
                Arguments = "\"" + Path.GetFullPath(name).Replace("\"", "\"\"") + "\"",
                Title = Path.GetFileName(name)
            };
            JumpList.AddToRecentCategory(task);
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecentFiles)));
        }

        private void JumpList_JumpItemsRejected(object sender, JumpItemsRejectedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void JumpList_JumpItemsRemovedByUser(object sender, JumpItemsRemovedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            this.Package = null;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!this.PromptUnsaved())
            {
                e.Cancel = true;
            }
        }

        private bool PromptUnsaved()
        {
            if (!this.OperationsAllowed)
            {
                return false;
            }

            if (this.Package == null || !this.Package.Dirty)
            {
                return true;
            }

            switch (MessageBox.Show(this, "You have unsaved changes. Do you want to save before performing this action?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.None, MessageBoxResult.Cancel))
            {
                case MessageBoxResult.Yes:
                    Commands.Save.Execute(this, this);
                    return !this.Package.Dirty;
                case MessageBoxResult.No:
                    return true;
                case MessageBoxResult.Cancel:
                default:
                    return false;
            }
        }
    }
}
