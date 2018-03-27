using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace UniversalPackageExplorer
{
    public struct RecentItem
    {
        internal RecentItem(string displayName, DateTime lastAccessed, string path)
        {
            this.DisplayName = displayName;
            this.LastAccessed = lastAccessed;
            this.Path = path;
        }

        public string DisplayName { get; }
        public DateTime LastAccessed { get; }
        public string Path { get; }
    }

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
                    openCommand.SetValue(string.Empty, "\"" + assembly.Location.Replace("\"", "\"\"") + "\" \"%1\"");
                }
                using (var supportedTypes = upe.CreateSubKey(@"SupportedTypes"))
                {
                    supportedTypes.SetValue(".upack", string.Empty, RegistryValueKind.String);
                }
            }
        }

        public static void StoreRecentEndpoints(string[] endpoints)
        {
            Registry.CurrentUser.SetValue(@"Software\Inedo\UniversalPackageExplorer\RecentEndpoints", endpoints, RegistryValueKind.MultiString);
        }

        public static string[] LoadRecentEndpoints()
        {
            return Registry.CurrentUser.GetValue(@"Software\Inedo\UniversalPackageExplorer\RecentEndpoints") as string[] ?? new string[0];
        }

        public static List<RecentItem> GetRecentItems()
        {
            var assembly = Assembly.GetEntryAssembly();

            using (var recentApps = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search\RecentApps"))
            {
                // TODO: figure out where the GUID comes from
                foreach (var appGuid in recentApps.GetSubKeyNames())
                {
                    using (var app = recentApps.OpenSubKey(appGuid))
                    {
                        if (string.Equals(app.GetValue("AppId") as string, assembly.Location, StringComparison.OrdinalIgnoreCase))
                        {
                            using (var recentItems = app.OpenSubKey(@"RecentItems"))
                            {
                                var items = new List<RecentItem>(recentItems.SubKeyCount);

                                foreach (var itemGuid in recentItems.GetSubKeyNames())
                                {
                                    using (var item = recentItems.OpenSubKey(itemGuid))
                                    {
                                        var type = (int)item.GetValue("Type");
                                        if (type == 0)
                                        {
                                            var displayName = (string)item.GetValue("DisplayName");
                                            var lastAccessed = DateTime.FromFileTime((long)item.GetValue("LastAccessedTime"));
                                            var path = (string)item.GetValue("Path");

                                            if (File.Exists(path))
                                            {
                                                items.Add(new RecentItem(displayName, lastAccessed, path));
                                            }
                                        }
                                    }
                                }

                                items.Sort((a, b) => b.LastAccessed.CompareTo(a.LastAccessed));

                                return items;
                            }
                        }
                    }
                }
            }

            return new List<RecentItem>();
        }
    }
}
