using System;
using System.Linq;
using System.Windows;

namespace Inedo.UniversalPackageExplorer.Uninstaller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() => this.InitializeComponent();

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Contains("/S", StringComparer.OrdinalIgnoreCase))
            {
                UninstallUniversalPackageExplorerTask.UninstallAsync().GetAwaiter().GetResult();
                return 0;
            }

            return new App().Run();
        }
    }
}
