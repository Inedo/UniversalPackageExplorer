using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using Inedo.Installer;
using Inedo.Installer.Subtasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Inedo.UniversalPackageExplorer.Setup
{
    public sealed class InstallUniversalPackageExplorerTask : InstallationTask<UniversalPackageExplorerInstallerOptions>
    {
        protected override void Install()
        {
#if DEBUG
            return;
#pragma warning disable CS0162 // Unreachable code detected
#endif
            this.LogDebug("Installing Universal Package Explorer...");
#if DEBUG
#pragma warning restore CS0162 // Unreachable code detected
#endif

            this.InstallFiles();
            this.EnsurePath();
        }

        private void InstallFiles()
        {
            this.RunSubtask(
                new CopyFilesSubtask
                {
                    SourceDirectory = "UniversalPackageExplorer",
                    TargetDirectory = this.Options.TargetPath
                }
            );
        }

        private void EnsurePath()
        {
            var upePath = this.Options.TargetPath;
            var variableTarget = this.Options.UserMode ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Machine;

            var path = Environment.GetEnvironmentVariable("PATH", variableTarget);
            if (addToPath(ref path))
                Environment.SetEnvironmentVariable("PATH", path, variableTarget);

            bool addToPath(ref string p)
            {
                if (!Regex.IsMatch(path, "(^|;)" + Regex.Escape(upePath.TrimEnd('\\')) + @"\?(;|$)", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                {
                    p = p.TrimEnd(';') + ";" + upePath;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void EnsureDirectory(string path, params string[] userAccounts)
        {
            this.LogInformation($"Creating {path}...");
            Directory.CreateDirectory(path);

            foreach (var userAccount in userAccounts.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    this.LogInformation($"Setting ACL on {path} for {userAccount}...");
                    var acl = Directory.GetAccessControl(path);
                    acl.AddAccessRule(new FileSystemAccessRule(userAccount, FileSystemRights.Read, AccessControlType.Allow));
                    acl.AddAccessRule(new FileSystemAccessRule(userAccount, FileSystemRights.Write, AccessControlType.Allow));
                    acl.AddAccessRule(new FileSystemAccessRule(userAccount, FileSystemRights.ListDirectory, AccessControlType.Allow));
                    Directory.SetAccessControl(path, acl);
                }
                catch (Exception ex)
                {
                    this.LogWarning("Unable to set ACL: " + ex.ToString());
                }
            }
        }

        private static void SplitUserAccount(string userAccount, out string userName, out string domainName)
        {
            var parts = userAccount.Split('\\');
            if (parts.Length == 1)
            {
                userName = parts[0];
                domainName = Environment.MachineName;
            }
            else if (parts.Length == 2)
            {
                userName = parts[1];
                domainName = parts[0];
            }
            else
            {
                userName = userAccount;
                domainName = Environment.MachineName;
            }
        }
    }
}
