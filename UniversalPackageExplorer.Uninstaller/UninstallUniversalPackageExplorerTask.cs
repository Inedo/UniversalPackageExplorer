using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Inedo.UniversalPackageExplorer.Uninstaller
{
    internal static class UninstallUniversalPackageExplorerTask
    {
        public static Task UninstallAsync() => UninstallAsync(UninstallOptions.Instance);
        public static Task UninstallAsync(UninstallOptions options)
        {
            DeleteDirectory(options.TargetPath);

            Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Universal Package Explorer", false);
            DeleteDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), @"Inedo\Universal Package Explorer"));
            try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Inedo"), false); }
            catch { }

            FinishUninstall();

            return Task.FromResult<object>(null);
        }

        private static void RunProcess(string fileName, string arguments)
        {
            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            );

            process.Start();
            process.WaitForExit();
        }
        private static void DeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
                return;
            }
            catch { }

            foreach (var fileName in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (UnauthorizedAccessException)
                {
                    try
                    {
                        File.SetAttributes(fileName, FileAttributes.Normal);
                        File.Delete(fileName);
                    }
                    catch
                    {
                    }
                }
                catch
                {
                }
            }

            foreach (var directoryName in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
            {
                try { Directory.Delete(directoryName, true); }
                catch { }
            }
        }

        private static void FinishUninstall()
        {
            try
            {
                var fileName = Path.Combine(Path.GetTempPath(), "FinishUPEUninstall.exe");
                using (var resourceStream = typeof(UninstallUniversalPackageExplorerTask).Assembly.GetManifestResourceStream("Inedo.UniversalPackageExplorer.Uninstaller.FinishUninstall.exe"))
                using (var targetStream = File.Create(fileName))
                {
                    resourceStream.CopyTo(targetStream);
                }

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = $"{Process.GetCurrentProcess().Id} \"{typeof(UninstallUniversalPackageExplorerTask).Assembly.Location}\" \"{Path.GetDirectoryName(typeof(UninstallUniversalPackageExplorerTask).Assembly.Location)}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                );
            }
            catch
            {
            }
        }
    }
}
