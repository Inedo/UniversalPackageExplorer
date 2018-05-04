using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UniversalPackageExplorer.UPack;

namespace UniversalPackageExplorer
{
    /// <summary>
    /// Interaction logic for FileTree.xaml
    /// </summary>
    public partial class FileTree : UserControl, INotifyPropertyChanged
    {
        public FileTree()
        {
            InitializeComponent();

            this.Tree.SelectedItemChanged += (s, e) =>
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty OperationsAllowedProperty = DependencyProperty.Register(nameof(OperationsAllowed), typeof(bool), typeof(FileTree));
        [Bindable(true)]
        public bool OperationsAllowed
        {
            get => (bool)this.GetValue(OperationsAllowedProperty);
            set => this.SetValue(OperationsAllowedProperty, value);
        }

        public static readonly DependencyProperty PackageProperty = DependencyProperty.Register(nameof(Package), typeof(UniversalPackage), typeof(FileTree));
        [Bindable(true)]
        public UniversalPackage Package
        {
            get => (UniversalPackage)this.GetValue(PackageProperty);
            set => this.SetValue(PackageProperty, value);
        }

        public UniversalPackageFile SelectedItem
        {
            get => (UniversalPackageFile)this.Tree.SelectedItem;
            set
            {
                if (value != null)
                {
                    this.FocusInTree(value);
                    return;
                }

                var selected = this.SelectedItem;
                if (selected != null)
                {
                    this.GetInTree(selected).IsSelected = false;
                }
            }
        }

        private void Tree_ContextMenuUnfocus(object sender, ContextMenuEventArgs e)
        {
            if (e.Source != sender)
            {
                return;
            }
            if (this.Tree.SelectedItem != null)
            {
                this.GetInTree((UniversalPackageFile)((TreeViewItem)this.Tree.SelectedItem).DataContext).IsSelected = false;
            }
        }

        private void Tree_ContextMenuFocus(object sender, ContextMenuEventArgs e)
        {
            this.FocusInTree((UniversalPackageFile)((FileTreeItem)sender).DataContext);
        }

        private bool Tree_FromSender(object sender, out UniversalPackage.FileCollection collection, out string prefix)
        {
            if (sender is TreeViewItem)
            {
                collection = this.Package.Metadata;
                prefix = string.Empty;
            }
            else if (sender is FileTreeItem item)
            {
                var file = (UniversalPackageFile)item.DataContext;
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

        private void Tree_DragOver(object sender, DragEventArgs e)
        {
            if (!this.OperationsAllowed)
            {
                e.Effects = DragDropEffects.None;
            }
            else if (!Tree_FromSender(sender, out var collection, out var prefix))
            {
                e.Handled = false;
                return;
            }
            else if (e.Data.GetDataPresent(typeof(UniversalPackageFile).FullName))
            {
                var file = (UniversalPackageFile)e.Data.GetData(typeof(UniversalPackageFile).FullName);
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

        private void Tree_Drop(object sender, DragEventArgs e)
        {
            if (!this.OperationsAllowed || e.Effects == DragDropEffects.None)
            {
                e.Handled = false;
                return;
            }

            if (!Tree_FromSender(sender, out var collection, out var prefix))
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
                    var file = await (await this.Dispatcher.InvokeAsync(() => MainWindow.Instance)).CreateFilePromptAsync(collection, namePrefix, info.Name);
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
                    var directory = await MainWindow.Instance.CreateDirectoryPromptAsync(collection, namePrefix, dir.Name);
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
            var collectionItem = this.Tree.Items.Cast<TreeViewItem>().Single(i => i.ItemsSource == file.Collection.Root);
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

        private void Tree_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var file = (UniversalPackageFile)this.Tree.SelectedItem;
                var panel = this.GetInTree(file);
                var dataObject = new DataObject(typeof(UniversalPackageFile).FullName, file, false);
                this.OperationsAllowed = false;
                Task.Run(async () =>
                {
                    dataObject.SetFileDropList(new StringCollection { await file.ExportTempFileAsync() });

                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.OperationsAllowed = true;
                        DragDrop.DoDragDrop(panel, dataObject, DragDropEffects.All);
                    });
                });
            }
        }

        private void FileTreeItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.Source != sender)
            {
                return;
            }

            this.FocusInTree((UniversalPackageFile)this.Tree.SelectedItem);

            Commands.OpenFileInWindowsShell.Execute(this, this);
        }
    }
}
