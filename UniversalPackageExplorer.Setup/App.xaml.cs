using System;
using System.Net;
using Inedo.Installer;

namespace Inedo.UniversalPackageExplorer.Setup
{
    public partial class App : InstallerApplication
    {
        public App() => this.InitializeComponent();

        [STAThread]
        public static int Main(string[] args)
        {
            ServicePointManager.Expect100Continue = false;
            return Run<UniversalPackageExplorerInstallerOptions, App, InstallUniversalPackageExplorerTask>(args);
        }
    }
}
