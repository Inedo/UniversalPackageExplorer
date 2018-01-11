using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UniversalPackageExplorer
{
    partial class MainWindow
    {
        private void FileTree_ContextMenuUnfocus(object sender, ContextMenuEventArgs e)
        {
            if (e.Source != sender)
            {
                return;
            }
            if (this.FileTree.SelectedItem != null)
            {
                this.GetInTree((UniversalPackageFile)this.FileTree.SelectedItem).IsSelected = false;
            }
        }

        private void FileTree_ContextMenuFocus(object sender, ContextMenuEventArgs e)
        {
            this.FocusInTree((UniversalPackageFile)((TextBlock)sender).DataContext);
        }

        private bool FileTree_FromSender(object sender, out UniversalPackage.FileCollection collection, out string prefix)
        {
            if (sender is TreeViewItem)
            {
                collection = this.Package.Metadata;
                prefix = string.Empty;
            }
            else if (sender is TextBlock text)
            {
                var file = (UniversalPackageFile)text.DataContext;
                collection = file.Collection;
                if (file.Children == null)
                {
                    var i = file.FullName.LastIndexOf('/');
                    prefix = file.FullName.Substring(0, i + 1);
                }
                else
                {
                    prefix = file.FullName;
                }
            }
            else if (sender is TreeView)
            {
                collection = this.Package.Files;
                prefix = string.Empty;
            }
            else
            {
                collection = null;
                prefix = null;
                return false;
            }
            return true;
        }

        private void FileTree_DragOver(object sender, DragEventArgs e)
        {
            if (!this.OperationsAllowed)
            {
                e.Effects = DragDropEffects.None;
            }
            else if (!FileTree_FromSender(sender, out var collection, out var prefix))
            {
                e.Handled = false;
                return;
            }
            else if (e.Data.GetDataPresent(typeof(UniversalPackageFile)))
            {
                var file = (UniversalPackageFile)e.Data.GetData(typeof(UniversalPackageFile));
                if (file.Collection != collection)
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else if (file.FullName.EndsWith("/", StringComparison.OrdinalIgnoreCase) && prefix.StartsWith(file.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    e.Effects = DragDropEffects.None;
                }
                else if (collection.ContainsKey(prefix + file.Name) || collection.ContainsKey(prefix + file.Name + "/"))
                {
                    e.Effects = DragDropEffects.None;
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                }
            }
            else if (e.Data.GetDataPresent("FileDrop"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void FileTree_Drop(object sender, DragEventArgs e)
        {
            if (!this.OperationsAllowed || e.Effects == DragDropEffects.None)
            {
                e.Handled = false;
                return;
            }

            if (!FileTree_FromSender(sender, out var collection, out var prefix))
            {
                e.Handled = false;
                return;
            }

            e.Handled = true;

            if (e.Effects == DragDropEffects.Move)
            {
                var file = (UniversalPackageFile)e.Data.GetData(typeof(UniversalPackageFile));
                if (file.Collection == collection)
                {
                    this.OperationsAllowed = false;
                    Task.Run(async () =>
                    {
                        await file.RenameAsync(prefix + file.Name);
                        await this.Dispatcher.InvokeAsync(() => this.OperationsAllowed = true);
                    });
                    return;
                }
            }

            var infos = ((string[])e.Data.GetData("FileDrop")).Select(f => File.GetAttributes(f).HasFlag(FileAttributes.Directory) ? (FileSystemInfo)new DirectoryInfo(f) : new FileInfo(f)).ToArray();
            this.OperationsAllowed = false;
            Task.Run(async () =>
            {
                UniversalPackageFile firstFile = null;

                foreach (var info in infos)
                {
                    UniversalPackageFile added;
                    if (info is DirectoryInfo dir)
                    {
                        added = await addDirectory(prefix, dir);
                    }
                    else
                    {
                        added = await addFile(prefix, (FileInfo)info);
                    }

                    firstFile = firstFile ?? added;
                }

                await this.Dispatcher.InvokeAsync(() =>
                {
                    if (firstFile != null)
                    {
                        this.FocusInTree(firstFile);
                    }
                    this.OperationsAllowed = true;
                });

                async Task<UniversalPackageFile> addFile(string namePrefix, FileInfo info)
                {
                    var file = await CreateFilePromptAsync(collection, namePrefix, info.Name);
                    if (file == null)
                    {
                        return null;
                    }

                    using (var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        await file.CopyFromAsync(stream);
                    }

                    return file;
                }

                async Task<UniversalPackageFile> addDirectory(string namePrefix, DirectoryInfo dir)
                {
                    var directory = await CreateDirectoryPromptAsync(collection, namePrefix, dir.Name);
                    if (directory == null)
                    {
                        return null;
                    }

                    foreach (var info in dir.GetFileSystemInfos())
                    {
                        if (info is DirectoryInfo subdir)
                        {
                            await addDirectory(directory.FullName, subdir);
                        }
                        else
                        {
                            await addFile(directory.FullName, (FileInfo)info);
                        }
                    }

                    return directory;
                }
            });
        }

        private TreeViewItem GetInTree(UniversalPackageFile file)
        {
            var collectionItem = this.FileTree.Items.Cast<TreeViewItem>().Single(i => i.ItemsSource == file.Collection.Root);
            var (children, view) = file.FullName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Aggregate((children: file.Collection.Root, view: collectionItem), (item, name) =>
            {
                var child = item.children[name];
                return (child.Children, (TreeViewItem)item.view.ItemContainerGenerator.ContainerFromItem(child));
            });
            return view;
        }

        private void FocusInTree(UniversalPackageFile file)
        {
            this.GetInTree(file).Focus();
        }

        private void FileTree_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var text = (TextBlock)e.Source;
                var file = (UniversalPackageFile)text.DataContext;
                var dataObject = new DataObject(typeof(UniversalPackageFile).FullName, file, false);
                this.OperationsAllowed = false;
                Task.Run(async () =>
                {
                    dataObject.SetFileDropList(new StringCollection { await ExportTempFileAsync(file) });

                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.OperationsAllowed = true;
                        try
                        {
                            DragDrop.DoDragDrop(text, dataObject, DragDropEffects.All);
                        }
                        catch
                        {
                        }
                    });
                });
            }
        }

        private string CreateTempDirectory()
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

        private async Task<string> ExportTempFileAsync(UniversalPackageFile file)
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

        private async Task<string> ExportTempFileAsync(UniversalPackage.FileCollection.SubTreeCollection dir)
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

        private async Task<string> ExportTempFileAsync(string basePath, UniversalPackageFile file)
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
