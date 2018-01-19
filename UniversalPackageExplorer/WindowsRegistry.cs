using Microsoft.Win32;
using System.Reflection;

namespace UniversalPackageExplorer
{
    internal static class WindowsRegistry
    {
        public static void AssociateWithUPackFiles()
        {
            var assembly = Assembly.GetEntryAssembly();

            using (var upe = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\UniversalPackageExplorer.exe"))
            {
                upe.SetValue(string.Empty, assembly.Location, RegistryValueKind.String);
            }

            using (var upe = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications\UniversalPackageExplorer.exe"))
            {
                using (var openCommand = upe.CreateSubKey(@"shell\open\command"))
                {
                    openCommand.SetValue(string.Empty, "\"" + assembly.Location + "\" \"%1\"");
                }
                using (var supportedTypes = upe.CreateSubKey(@"SupportedTypes"))
                {
                    supportedTypes.SetValue(".upack", string.Empty, RegistryValueKind.String);
                }
            }
        }
    }
}
