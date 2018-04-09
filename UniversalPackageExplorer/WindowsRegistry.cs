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
            var openCommand = "\"" + assembly.Location.Replace("\"", "\"\"") + "\" \"%1\"";

            using (var upe = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\UniversalPackageExplorer.exe"))
            {
                upe.SetValue(string.Empty, assembly.Location, RegistryValueKind.String);
            }

            using (var upe = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Applications\UniversalPackageExplorer.exe"))
            {
                using (var command = upe.CreateSubKey(@"shell\open\command"))
                {
                    command.SetValue(string.Empty, openCommand);
                }
                using (var supportedTypes = upe.CreateSubKey(@"SupportedTypes"))
                {
                    supportedTypes.SetValue(".upack", string.Empty, RegistryValueKind.String);
                }
            }

            string key;
            using (var upack = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.upack"))
            {
                key = upack.GetValue(string.Empty) as string;
                if (string.IsNullOrEmpty(key))
                {
                    key = "inedo_upack_file";
                    upack.SetValue(string.Empty, key);
                }
            }

            using (var command = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + key + @"\shell\open\command"))
            {
                command.SetValue(string.Empty, openCommand);
            }
        }

        public static bool IsAssociatedWithUPackFiles()
        {
            var assembly = Assembly.GetEntryAssembly();
            var openCommand = "\"" + assembly.Location.Replace("\"", "\"\"") + "\" \"%1\"";

            using (var upe = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\UniversalPackageExplorer.exe", false))
            {
                if (upe?.GetValue(string.Empty) as string != assembly.Location)
                {
                    return false;
                }
            }

            using (var upe = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Applications\UniversalPackageExplorer.exe", false))
            {
                if (upe == null)
                {
                    return false;
                }

                using (var command = upe.OpenSubKey(@"shell\open\command", false))
                {
                    if (command?.GetValue(string.Empty) as string != openCommand)
                    {
                        return false;
                    }
                }
                using (var supportedTypes = upe.OpenSubKey(@"SupportedTypes", false))
                {
                    if (supportedTypes?.GetValueKind(".upack") != RegistryValueKind.String)
                    {
                        return false;
                    }
                }
            }

            string key;
            using (var upack = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.upack", false))
            {
                key = upack?.GetValue(string.Empty) as string;
                if (string.IsNullOrEmpty(key))
                {
                    return false;
                }
            }

            using (var command = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + key + @"\shell\open\command", false))
            {
                if (command?.GetValue(string.Empty) as string != openCommand)
                {
                    return false;
                }
            }

            return true;
        }

        public static void StoreRecentEndpoints(string[] endpoints)
        {
            using (var upe = Registry.CurrentUser.CreateSubKey(@"Software\Inedo\UniversalPackageExplorer"))
            {
                upe.SetValue("RecentEndpoints", endpoints, RegistryValueKind.MultiString);
            }
        }

        public static string[] LoadRecentEndpoints()
        {
            using (var upe = Registry.CurrentUser.OpenSubKey(@"Software\Inedo\UniversalPackageExplorer", false))
            {
                return upe?.GetValue("RecentEndpoints") as string[] ?? new string[0];
            }
        }

        public static void StoreDontAskToAssociate()
        {
            using (var upe = Registry.CurrentUser.CreateSubKey(@"Software\Inedo\UniversalPackageExplorer"))
            {
                upe.SetValue("DontAskToAssociate", 1, RegistryValueKind.DWord);
            }
        }

        public static bool LoadDontAskToAssociate()
        {
            using (var upe = Registry.CurrentUser.OpenSubKey(@"Software\Inedo\UniversalPackageExplorer", false))
            {
                return (int?)upe?.GetValue("DontAskToAssociate", 0) == 1;
            }
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
