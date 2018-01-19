using System;
using System.IO;
using System.Threading.Tasks;

namespace UniversalPackageExplorer.UPack
{
    public static class UniversalPackageExtensions
    {
        private static string CreateTempDirectory()
        {
            var baseTempDir = new DirectoryInfo(Path.GetTempPath());
            while (true)
            {
                var name = Path.GetRandomFileName();
                try
                {
                    return baseTempDir.CreateSubdirectory(name).FullName;
                }
                catch (IOException)
                {
                    // already exists; try a different name.
                }
            }
        }

        public static async Task<string> ExportTempFileAsync(this UniversalPackageFile file)
        {
            var tempDir = CreateTempDirectory();
            var fileName = await ExportTempFileAsync(tempDir, file);
            AppDomain.CurrentDomain.DomainUnload += (s, _e) =>
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            };
            return fileName;
        }

        public static async Task<string> ExportTempFileAsync(this UniversalPackage.FileCollection.SubTreeCollection dir)
        {
            var tempDir = CreateTempDirectory();
            foreach (var file in dir)
            {
                await ExportTempFileAsync(tempDir, file);
            }
            AppDomain.CurrentDomain.DomainUnload += (s, _e) =>
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            };
            return tempDir;
        }

        private static async Task<string> ExportTempFileAsync(string basePath, UniversalPackageFile file)
        {
            var fullName = Path.Combine(basePath, file.Name);
            if (file.Children == null)
            {
                using (var stream = new FileStream(fullName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await file.CopyToAsync(stream);
                }
            }
            else
            {
                Directory.CreateDirectory(fullName);
                foreach (var child in file.Children)
                {
                    await ExportTempFileAsync(fullName, child);
                }
            }

            return fullName;
        }
    }
}
