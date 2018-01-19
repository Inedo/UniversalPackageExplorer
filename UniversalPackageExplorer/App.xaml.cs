using System.Windows;

namespace UniversalPackageExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static string StartupPackage = null;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            WindowsRegistry.AssociateWithUPackFiles();

            if (e.Args.Length != 0)
            {
                StartupPackage = e.Args[0];
            }
        }
    }
}
