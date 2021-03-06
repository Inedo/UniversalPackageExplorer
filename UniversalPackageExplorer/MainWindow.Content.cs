﻿using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using UniversalPackageExplorer.UPack;

namespace UniversalPackageExplorer
{
    partial class MainWindow
    {
        private bool IsUPackJsonSelected => this.FileTree.SelectedItem != null && this.FileTree.SelectedItem.Collection == this.Package.Metadata && string.Equals(this.FileTree.SelectedItem.FullName, "upack.json", StringComparison.OrdinalIgnoreCase);
        private bool IsManifestEditorSelected => this.ManifestEditor.IsKeyboardFocusWithin;

        private void Content_CanOpenFile(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.FileTree.SelectedItem != null && this.FileTree.SelectedItem.Children == null && !this.IsManifestEditorSelected;
        }
        private void Content_ViewFileContent(object sender, ExecutedRoutedEventArgs e)
        {
            var file = this.FileTree.SelectedItem;

            Task.Run(() => this.PerformOperationAsync(async () =>
            {
                var fullName = await file.ExportTempFileAsync();

                var txtCommand = SafeNativeMethods.AssocQueryString(SafeNativeMethods.AssocStr.Command, ".txt");

                txtCommand = txtCommand.Replace("\"%1\"", "\"" + fullName.Replace("\"", "\"\"") + "\"");

                Process.Start(new ProcessStartInfo("cmd.exe", "/c start \"\" " + txtCommand) { UseShellExecute = false, CreateNoWindow = true }).Dispose();
            }));
        }
        private void Content_OpenFileInWindowsShell(object sender, ExecutedRoutedEventArgs e)
        {
            var file = this.FileTree.SelectedItem;

            Task.Run(() => this.PerformOperationAsync(async () =>
            {
                string fullName;
                if (file.Collection == this.Package.Metadata)
                {
                    fullName = await file.ExportTempFileAsync();
                }
                else
                {
                    var tempDir = await file.Collection.Root.ExportTempFileAsync();
                    fullName = Path.Combine(tempDir, file.FullName);
                }

                try
                {
                    Process.Start(new ProcessStartInfo(fullName) { UseShellExecute = true }).Dispose();
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1155) // ERROR_NO_ASSOCIATION
                {
                    Process.Start("rundll32.exe", "shell32.dll, OpenAs_RunDLL " + fullName).Dispose();
                }
            }));
        }
        private void Content_SaveFileAs(object sender, ExecutedRoutedEventArgs e)
        {
            var file = this.FileTree.SelectedItem;
            var ext = Path.GetExtension(file.Name);
            var filter = "All files (*.*)|*.*";

            if (ext.StartsWith("."))
            {
                try
                {
                    var name = SafeNativeMethods.AssocQueryString(SafeNativeMethods.AssocStr.FriendlyDocName, ext);
                    filter = name + " (*" + ext + ")|*" + ext + "|" + filter;
                }
                catch (InvalidOperationException)
                {
                    filter = ext.Substring(1) + " file (*" + ext + ")|*" + ext + "|" + filter;
                }
            }

            var dialog = new SaveFileDialog
            {
                ValidateNames = true,
                OverwritePrompt = true,
                DefaultExt = ext,
                Filter = filter,
                FileName = file.Name
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            Task.Run(() => this.PerformOperationAsync(async () =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dialog.FileName));
                using (var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await file.CopyToAsync(stream);
                }
            }));
        }

        private void Content_NewFile(object sender, ExecutedRoutedEventArgs e)
        {
            var referenceFile = this.FileTree.SelectedItem;
            var collection = referenceFile == null ? this.Package.Files : referenceFile.Collection;
            var prefix = referenceFile == null ? string.Empty : referenceFile.FullName.Substring(0, referenceFile.FullName.LastIndexOf('/') + 1);

            var prompt = new NamePromptWindow("New File", "New file name:", NamePromptWindow.CreateNameValidator(false, collection, collection == this.Package.Metadata, prefix))
            {
                Owner = this
            };

            if (prompt.ShowDialog() == true)
            {
                Task.Run(() => this.PerformOperationAsync(() => collection.CreateFileAsync(prefix + prompt.Text), this.FocusInTree));
            }
        }

        private void Content_ExistingFile(object sender, ExecutedRoutedEventArgs e)
        {
            var referenceFile = this.FileTree.SelectedItem;
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
                Task.Run(() => this.PerformOperationAsync(async () =>
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
                                var prompt = new NamePromptWindow("Existing File", "New file name:", NamePromptWindow.CreateNameValidator(false, collection, collection == this.Package.Metadata, prefix))
                                {
                                    Text = Path.GetFileName(fileName),
                                    Owner = this
                                };

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

                    return firstFile;
                }, firstFile =>
                {
                    if (firstFile != null)
                    {
                        this.FocusInTree(firstFile);
                    }
                }));
            }
        }

        private void Content_NewFolder(object sender, ExecutedRoutedEventArgs e)
        {
            var referenceFile = this.FileTree.SelectedItem;
            var collection = referenceFile == null ? this.Package.Files : referenceFile.Collection;
            var prefix = referenceFile == null ? string.Empty : referenceFile.FullName.Substring(0, referenceFile.FullName.LastIndexOf('/') + 1);

            var prompt = new NamePromptWindow("New Folder", "New folder name:", NamePromptWindow.CreateNameValidator(true, collection, collection == this.Package.Metadata, prefix))
            {
                Owner = this
            };

            if (prompt.ShowDialog() == true)
            {
                Task.Run(() => this.PerformOperationAsync(() => collection.CreateDirectoryAsync(prefix + prompt.Text + "/"), this.FocusInTree));
            }
        }

        private void Content_ExistingFolder(object sender, ExecutedRoutedEventArgs e)
        {
            var referenceFile = this.FileTree.SelectedItem;
            var collection = referenceFile == null ? this.Package.Files : referenceFile.Collection;
            var prefix = referenceFile == null ? string.Empty : referenceFile.FullName.Substring(0, referenceFile.FullName.LastIndexOf('/') + 1);

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = false
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var folderName = dialog.SelectedPath;
                Task.Run(() => this.PerformOperationAsync(async () =>
                {
                    UniversalPackageFile rootFolder;
                    try
                    {
                        rootFolder = await collection.CreateDirectoryAsync(prefix + Path.GetFileName(folderName));
                    }
                    catch (ArgumentException)
                    {
                        var name = await this.Dispatcher.InvokeAsync(() =>
                        {
                            var prompt = new NamePromptWindow("Existing File", "New file name:", NamePromptWindow.CreateNameValidator(true, collection, collection == this.Package.Metadata, prefix))
                            {
                                Text = Path.GetFileName(folderName),
                                Owner = this
                            };

                            if (prompt.ShowDialog() == true)
                            {
                                return prompt.Text;
                            }

                            return null;
                        });

                        if (name == null)
                        {
                            rootFolder = null;
                        }
                        else
                        {
                            rootFolder = await collection.CreateDirectoryAsync(prefix + Path.GetFileName(folderName));
                        }
                    }

                    await rootFolder?.ImportFromFileSystemAsync(folderName);

                    return rootFolder;
                }, rootFolder =>
                {
                    if (rootFolder != null)
                    {
                        this.FocusInTree(rootFolder);
                    }
                }));
            }
        }

        private void Content_CanRename(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.OperationsAllowed && this.FileTree.SelectedItem != null && !this.IsUPackJsonSelected && !this.IsManifestEditorSelected;
        }
        private void Content_Rename(object sender, ExecutedRoutedEventArgs e)
        {
            var file = this.FileTree.SelectedItem;
            var prefix = file.FullName.Substring(0, file.FullName.LastIndexOf('/') + 1);

            var prompt = new NamePromptWindow("Rename", file.Children == null ? "Rename file to:" : "Rename folder to:", NamePromptWindow.CreateNameValidator(file.Children != null, file.Collection, file.Collection == this.Package.Metadata, prefix, file.Name))
            {
                Text = file.Name,
                Owner = this
            };

            if (prompt.ShowDialog() == true)
            {
                Task.Run(() => this.PerformOperationAsync(async () =>
                {
                    await file.RenameAsync(prefix + prompt.Text);
                    return file;
                }, this.FocusInTree));
            }
        }

        private void Content_CanDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.OperationsAllowed && this.FileTree.SelectedItem != null && !this.IsUPackJsonSelected && !this.IsManifestEditorSelected;
        }
        private void Content_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            var file = this.FileTree.SelectedItem;

            var confirm = new ConfirmationWindow(file.Children == null ? "Delete file?" : "Delete folder?", $"Are you sure you want to delete '{file.Name}'? This cannot be undone.", cancelText: null)
            {
                Owner = this
            };

            if (confirm.ShowDialog() == true)
            {
                Task.Run(() => this.PerformOperationAsync(file.DeleteAsync));
            }
        }

        private void Content_CanRemoveFolder(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.OperationsAllowed && this.FileTree.SelectedItem != null && this.FileTree.SelectedItem.Children != null && !this.IsUPackJsonSelected && !this.IsManifestEditorSelected;
        }
        private void Content_RemoveFolder(object sender, ExecutedRoutedEventArgs e)
        {
            var file = this.FileTree.SelectedItem;

            var confirm = new ConfirmationWindow("Remove folder?", $"Are you sure you want to move the contents of '{file.Name}' to the parent folder? This cannot be undone.", cancelText: null)
            {
                Owner = this
            };

            if (confirm.ShowDialog() == true)
            {
                Task.Run(() => this.PerformOperationAsync(async () =>
                {
                    var dir = file.FullName.Substring(0, file.FullName.Length - file.Name.Length - 1);
                    var children = file.Children.ToArray();
                    foreach (var c in children)
                    {
                        await c.RenameAsync(dir + c.Name);
                    }
                    await file.DeleteAsync();
                }));
            }
        }

        internal async Task<UniversalPackageFile> CreateFilePromptAsync(UniversalPackage.FileCollection collection, string prefix, string name)
        {
            try
            {
                return await collection.CreateFileAsync(prefix + name);
            }
            catch (ArgumentException)
            {
                name = await this.Dispatcher.InvokeAsync(() =>
                {
                    var prompt = new NamePromptWindow("File Name Conflict", "New file name:", NamePromptWindow.CreateNameValidator(false, collection, collection == this.Package.Metadata, prefix))
                    {
                        Text = name,
                        Owner = this
                    };

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

        internal async Task<UniversalPackageFile> CreateDirectoryPromptAsync(UniversalPackage.FileCollection collection, string prefix, string name)
        {
            try
            {
                return await collection.CreateDirectoryAsync(prefix + name);
            }
            catch (ArgumentException)
            {
                name = await this.Dispatcher.InvokeAsync(() =>
                {
                    var prompt = new NamePromptWindow("Folder Name Conflict", "New folder name:", NamePromptWindow.CreateNameValidator(true, collection, collection == this.Package.Metadata, prefix))
                    {
                        Text = name,
                        Owner = this
                    };

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

        private void FocusInTree(UniversalPackageFile file)
        {
            this.FileTree.SelectedItem = file;
        }
    }
}
