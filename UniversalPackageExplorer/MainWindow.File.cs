using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UniversalPackageExplorer.UPack;

namespace UniversalPackageExplorer
{
    partial class MainWindow
    {
        private void File_New(object sender, ExecutedRoutedEventArgs e)
        {
            if (!this.PromptUnsaved())
            {
                return;
            }

            this.Package = new UniversalPackage();
        }
        private async void File_Open(object sender, ExecutedRoutedEventArgs e)
        {
            if (!this.PromptUnsaved())
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                ValidateNames = true,
                ShowReadOnly = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "UPack files (*.upack)|*.upack|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) == true)
            {
                using (var file = dialog.OpenFile())
                {
                    this.AddRecentFile(dialog.FileName);
                    if (dialog.ReadOnlyChecked)
                    {
                        this.Package = await UniversalPackage.CreateAsync(file);
                    }
                    else
                    {
                        this.Package = await UniversalPackage.CreateAsync(dialog.FileName, file);
                    }
                }
            }
        }
        private void File_OpenFromFeed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!this.PromptUnsaved())
            {
                return;
            }

            var dialog = new OpenFromFeedWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                this.OperationsAllowed = false;
                Task.Run(async () =>
                {
                    var package = await dialog.DownloadAsync();

                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.Package = package;
                        this.OperationsAllowed = true;
                    });
                });
            }
        }
        private void File_CanClose(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.OperationsAllowed && this.Package != null;
        }
        private void File_Close(object sender, ExecutedRoutedEventArgs e)
        {
            if (!this.PromptUnsaved())
            {
                return;
            }

            this.Package = null;
        }
        private void File_CanSave(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.OperationsAllowed && this.Package != null;
        }
        private void File_Save(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.Package.IsTemporary)
            {
                this.File_SaveAs(sender, e);
                return;
            }

            this.OperationsAllowed = false;
            Task.Run(async () =>
            {
                await this.Package.SaveAsync();
                await this.Dispatcher.InvokeAsync(() => this.OperationsAllowed = true);
            });
        }
        private void File_CanSaveAs(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.OperationsAllowed && this.Package != null;
        }
        private void File_SaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                ValidateNames = true,
                OverwritePrompt = true,
                AddExtension = true,
                FileName = $"{this.Package.Manifest.Name}-{this.Package.Manifest.VersionText}.upack",
                DefaultExt = ".upack",
                Filter = "UPack files (*.upack)|*.upack|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            this.Package.FullName = dialog.FileName;
            this.OperationsAllowed = false;
            Task.Run(async () =>
            {
                await this.Package.SaveAsync();
                await this.Dispatcher.InvokeAsync(() =>
                {
                    this.AddRecentFile(this.Package.FullName);
                    this.OperationsAllowed = true;
                });
            });
        }

        private void File_CanPublish(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.OperationsAllowed && this.Package != null;
        }
        private void File_Publish(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new PublishWindow { Owner = this };
            dialog.Show();

            this.OperationsAllowed = false;
            Task.Run(async () =>
            {
                var tempCopy = await this.Package.CopyToTempAsync();

                await this.Dispatcher.InvokeAsync(() =>
                {
                    this.OperationsAllowed = true;

                    if (!dialog.IsVisible)
                    {
                        tempCopy.Dispose();
                        return;
                    }

                    dialog.FileToUpload = tempCopy;
                });
            });
        }

        private async void File_OpenRecentFile(object sender, ExecutedRoutedEventArgs e)
        {
            if (!this.PromptUnsaved())
            {
                return;
            }

            var path = (string)e.Parameter;
            this.Package = await UniversalPackage.CreateAsync(path);
            this.AddRecentFile(path);
        }

        private void File_Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
