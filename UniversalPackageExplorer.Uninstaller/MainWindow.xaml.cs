using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;

namespace Inedo.UniversalPackageExplorer.Uninstaller
{
    public partial class MainWindow : Window
    {
        private bool canClose = true;

        public MainWindow() => this.InitializeComponent();

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!this.canClose)
                e.Cancel = true;

            base.OnClosing(e);
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            this.canClose = false;
            UninstallOptions.Instance.Running = true;

            this.TaskbarItemInfo = new TaskbarItemInfo { ProgressState = TaskbarItemProgressState.Indeterminate };

            await Task.Run(UninstallUniversalPackageExplorerTask.UninstallAsync);

            this.canClose = true;
            this.Close();
        }
    }
}
