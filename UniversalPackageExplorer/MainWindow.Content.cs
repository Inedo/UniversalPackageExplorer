using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace UniversalPackageExplorer
{
    partial class MainWindow
    {
        private bool IsUPackJsonSelected => this.FileTree.SelectedItem != null && ((UniversalPackageFile)this.FileTree.SelectedItem).Collection == this.Package.Metadata && string.Equals(((UniversalPackageFile)this.FileTree.SelectedItem).FullName, "upack.json", StringComparison.OrdinalIgnoreCase);
        private Func<string, string> ValidateName(bool isDirectory, UniversalPackage.FileCollection collection, string prefix, string existingName = null)
        {
            return validate;

            string validate(string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return "Enter a name.";
                }

                if (newName.Contains("/") || newName.Contains("\\"))
                {
                    return isDirectory ? "Folder names cannot contain slashes." : "File names cannot contain slashes.";
                }

                if (prefix == string.Empty && string.Equals(newName, "package", StringComparison.OrdinalIgnoreCase) && collection == this.Package.Metadata)
                {
                    return isDirectory ? "A metadata folder cannot be named \"package\"." : "A metadata file cannot be named \"package\".";
                }

                if (string.Equals(newName, existingName, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var fullName = prefix + newName;

                if (collection.ContainsKey(fullName))
                {
                    return "Name already in use!";
                }

                try
                {
                    collection.CheckPath(fullName);
                    return null;
                }
                catch (ArgumentException ex)
                {
                    return ex.Message;
                }
            };
        }

        private void Content_CanOpenFile(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.FileTree.SelectedItem != null && ((UniversalPackageFile)this.FileTree.SelectedItem).Children == null;
        }
        private void Content_ViewFileContent(object sender, ExecutedRoutedEventArgs e)
        {
            var file = (UniversalPackageFile)this.FileTree.SelectedItem;

            this.OperationsAllowed = false;
            Task.Run(async () =>
            {
                var fullName = await ExportTempFileAsync(file);

                var txtCommand = SafeNativeMethods.AssocQueryString(SafeNativeMethods.AssocStr.Command, ".txt");

                txtCommand = txtCommand.Replace("\"%1\"", "\"" + fullName.Replace("\"", "\"\"") + "\"");

                Process.Start(new ProcessStartInfo("cmd.exe", "/c start \"\" " + txtCommand) { UseShellExecute = false, CreateNoWindow = true }).Dispose();

                await this.Dispatcher.InvokeAsync(() => this.OperationsAllowed = true);
            });
        }
        private void Content_OpenFileInWindowsShell(object sender, ExecutedRoutedEventArgs e)
        {
            var file = (UniversalPackageFile)this.FileTree.SelectedItem;

            this.OperationsAllowed = false;
            Task.Run(async () =>
            {
                if (file.Collection == this.Package.Metadata)
                {
                    var fullName = await ExportTempFileAsync(file);
                    Process.Start(new ProcessStartInfo(fullName) { UseShellExecute = true }).Dispose();
                }
                else
                {
                    var tempDir = await ExportTempFileAsync(file.Collection.Root);
                    var fullName = Path.Combine(tempDir, file.FullName);
                    Process.Start(new ProcessStartInfo(fullName) { UseShellExecute = true }).Dispose();
                }

                await this.Dispatcher.InvokeAsync(() => this.OperationsAllowed = true);
            });
        }

        private void Content_NewFile(object sender, ExecutedRoutedEventArgs e)
        {
            var referenceFile = (UniversalPackageFile)this.FileTree.SelectedItem;
            var collection = referenceFile == null ? this.Package.Files : referenceFile.Collection;
            var prefix = referenceFile == null ? string.Empty : referenceFile.FullName.Substring(0, referenceFile.FullName.LastIndexOf('/') + 1);

            var prompt = new NamePromptWindow("New File", "New file name:", ValidateName(false, collection, prefix));

            if (prompt.ShowDialog() == true)
            {
                this.OperationsAllowed = false;
                Task.Run(async () =>
                {
                    var file = await collection.CreateFileAsync(prefix + prompt.Text);
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.FocusInTree(file);
                        this.OperationsAllowed = true;
                    });
                });
            }
        }

        private void Content_ExistingFile(object sender, ExecutedRoutedEventArgs e)
        {
            var referenceFile = (UniversalPackageFile)this.FileTree.SelectedItem;
            var collection = referenceFile == null ? this.Package.Files : referenceFile.Collection;
            var prefix = referenceFile == null ? string.Empty : referenceFile.FullName.Substring(0, referenceFile.FullName.LastIndexOf('/') + 1);

            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = true,
                ValidateNames = true,
                Filter = "All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) == true)
            {
                this.OperationsAllowed = false;
                Task.Run(async () =>
                {
                    UniversalPackageFile firstFile = null;

                    foreach (var fileName in dialog.FileNames)
                    {
                        UniversalPackageFile file;
                        try
                        {
                            file = await collection.CreateFileAsync(prefix + Path.GetFileName(fileName));
                        }
                        catch (ArgumentException)
                        {
                            var name = await this.Dispatcher.InvokeAsync(() =>
                            {
                                var prompt = new NamePromptWindow("Existing File", "New file name:", ValidateName(false, collection, prefix)) { Text = Path.GetFileName(fileName) };

                                if (prompt.ShowDialog() == true)
                                {
                                    return prompt.Text;
                                }

                                return null;
                            });

                            if (name == null)
                            {
                                continue;
                            }

                            file = await collection.CreateFileAsync(prefix + name);
                        }

                        if (firstFile == null)
                        {
                            firstFile = file;
                        }

                        using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                        {
                            await file.CopyFromAsync(stream);
                        }
                    }

                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        if (firstFile != null)
                        {
                            this.FocusInTree(firstFile);
                        }
                        this.OperationsAllowed = true;
                    });
                });
            }
        }

        private void Content_NewFolder(object sender, ExecutedRoutedEventArgs e)
        {
            var referenceFile = (UniversalPackageFile)this.FileTree.SelectedItem;
            var collection = referenceFile == null ? this.Package.Files : referenceFile.Collection;
            var prefix = referenceFile == null ? string.Empty : referenceFile.FullName.Substring(0, referenceFile.FullName.LastIndexOf('/') + 1);

            var prompt = new NamePromptWindow("New Folder", "New folder name:", ValidateName(true, collection, prefix));

            if (prompt.ShowDialog() == true)
            {
                this.OperationsAllowed = false;
                Task.Run(async () =>
                {
                    var file = await collection.CreateDirectoryAsync(prefix + prompt.Text + "/");
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.FocusInTree(file);
                        this.OperationsAllowed = true;
                    });
                });
            }
        }

        private void Content_CanRename(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.OperationsAllowed && this.FileTree.SelectedItem != null && !this.IsUPackJsonSelected;
        }
        private void Content_Rename(object sender, ExecutedRoutedEventArgs e)
        {
            var file = (UniversalPackageFile)this.FileTree.SelectedItem;
            var prefix = file.FullName.Substring(0, file.FullName.LastIndexOf('/') + 1);

            var prompt = new NamePromptWindow("Rename", file.Children == null ? "Rename file to:" : "Rename folder to:", ValidateName(file.Children != null, file.Collection, prefix, file.Name)) { Text = file.Name };

            if (prompt.ShowDialog() == true)
            {
                this.OperationsAllowed = false;
                Task.Run(async () =>
                {
                    await file.RenameAsync(prefix + prompt.Text);
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.FocusInTree(file);
                        this.OperationsAllowed = true;
                    });
                });
            }
        }

        private void Content_CanDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.OperationsAllowed && this.FileTree.SelectedItem != null && !this.IsUPackJsonSelected;
        }
        private void Content_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            var file = (UniversalPackageFile)this.FileTree.SelectedItem;

            switch (MessageBox.Show(this, $"Are you sure you want to delete '{file.Name}'? This cannot be undone.", file.Children == null ? "Delete file?" : "Delete folder?", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No))
            {
                case MessageBoxResult.Yes:
                    this.OperationsAllowed = false;
                    Task.Run(async () =>
                    {
                        await file.DeleteAsync();
                        await this.Dispatcher.InvokeAsync(() => this.OperationsAllowed = true);
                    });
                    break;
                default:
                    break;
            }
        }

        private async Task<UniversalPackageFile> CreateFilePromptAsync(UniversalPackage.FileCollection collection, string prefix, string name)
        {
            try
            {
                return await collection.CreateFileAsync(prefix + name);
            }
            catch (ArgumentException)
            {
                name = await this.Dispatcher.InvokeAsync(() =>
                {
                    var prompt = new NamePromptWindow("File Name Conflict", "New file name:", ValidateName(false, collection, prefix)) { Text = name };

                    if (prompt.ShowDialog() == true)
                    {
                        return prompt.Text;
                    }

                    return null;
                });

                if (name == null)
                {
                    return null;
                }

                return await collection.CreateFileAsync(prefix + name);
            }
        }

        private async Task<UniversalPackageFile> CreateDirectoryPromptAsync(UniversalPackage.FileCollection collection, string prefix, string name)
        {
            try
            {
                return await collection.CreateDirectoryAsync(prefix + name);
            }
            catch (ArgumentException)
            {
                name = await this.Dispatcher.InvokeAsync(() =>
                {
                    var prompt = new NamePromptWindow("Folder Name Conflict", "New folder name:", ValidateName(true, collection, prefix)) { Text = name };

                    if (prompt.ShowDialog() == true)
                    {
                        return prompt.Text;
                    }

                    return null;
                });

                if (name == null)
                {
                    return null;
                }

                return await collection.CreateDirectoryAsync(prefix + name);
            }
        }
    }
}
