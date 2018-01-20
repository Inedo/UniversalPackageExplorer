using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using UniversalPackageExplorer.UPack;

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

        public static MainWindow Instance => (MainWindow)Application.Current.MainWindow;

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

        public List<RecentItem> RecentFiles => WindowsRegistry.GetRecentItems();

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
            JumpList.AddToRecentCategory(name);
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

            var confirm = new ConfirmationWindow("Unsaved Changes", "You have unsaved changes. Do you want to save before performing this action?")
            {
                Owner = this
            };

            bool? result = confirm.ShowDialog();
            if (!result.HasValue)
            {
                return false;
            }

            if (result.Value)
            {
                Commands.Save.Execute(this, this);
                return !this.Package.Dirty;
            }

            return true;
        }
    }
}
