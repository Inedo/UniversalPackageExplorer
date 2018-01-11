using System.Diagnostics;
using System.Windows.Input;

namespace UniversalPackageExplorer
{
    partial class MainWindow
    {
        private void Help_ProjectHome(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start("https://github.com/Inedo/UniversalPackageExplorer");
        }

        private void Help_FileReference(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start("https://inedo.com/support/documentation/various/universal-packages/universal-feeds-package-ref/package-format");
        }

        private void Help_About(object sender, ExecutedRoutedEventArgs e)
        {
            var about = new AboutWindow
            {
                Owner = this
            };

            about.ShowDialog();
        }
    }
}
