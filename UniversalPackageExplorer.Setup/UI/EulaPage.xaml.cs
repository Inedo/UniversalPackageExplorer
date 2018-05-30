using System.Windows;
using Inedo.Installer.UI;

namespace Inedo.UniversalPackageExplorer.Setup.UI
{
    public partial class EulaPage : InstallerPageBase
    {
        public EulaPage() => this.InitializeComponent();

        private void AgreeButton_Click(object sender, RoutedEventArgs e)
        {
            ((UniversalPackageExplorerInstallerOptions)this.InstallationOptions).AcceptsEula = true;
            this.NavigateToPage<ModePage>();
        }
    }
}
