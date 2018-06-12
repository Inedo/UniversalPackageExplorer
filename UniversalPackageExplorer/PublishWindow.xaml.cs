using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using UniversalPackageExplorer.UPack;

namespace UniversalPackageExplorer
{
    /// <summary>
    /// Interaction logic for PublishWindow.xaml
    /// </summary>
    public partial class PublishWindow : Window, INotifyPropertyChanged
    {
        public PublishWindow()
        {
            InitializeComponent();
            this.Closed += PublishWindow_Closed;
            this.CheckAuth();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> RecentEndpoints { get; } = new ObservableCollection<string>(WindowsRegistry.LoadRecentEndpoints());

        private string endpointUri = null;
        public string EndpointUri
        {
            get => this.endpointUri ?? this.RecentEndpoints.DefaultIfEmpty("https://proget.example.com/upack/FeedName").First();
            set
            {
                this.endpointUri = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndpointUri)));
                this.credentials = null;
                this.IsAuthRequired = false;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAuthRequired)));

                this.CheckAuth();
            }
        }

        private NetworkCredential credentials;

        public bool IsAuthRequired { get; private set; }
        public bool IsFeedValid { get; private set; }
        public Exception FailureMessage { get; private set; }

        private FileStream fileToUpload = null;
        public FileStream FileToUpload
        {
            get => this.fileToUpload;
            set
            {
                this.fileToUpload = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileToUpload)));

                this.CheckAuth();
            }
        }

        private void PublishWindow_Closed(object sender, System.EventArgs e)
        {
            this.FileToUpload?.Dispose();
            this.FileToUpload = null;
        }

        private bool isUpdating = false;
        private bool updateQueued = false;

        private void CheckAuth()
        {
            if (this.FileToUpload == null)
            {
                return;
            }

            if (this.isUpdating)
            {
                this.updateQueued = true;
                return;
            }

            this.isUpdating = true;

            var endpoint = this.EndpointUri;
            var auth = this.credentials;

            Task.Run(async () =>
            {
                string authError;
                bool isAuthRequired;
                bool isValid = true;
                try
                {
                    authError = await UniversalPackageEndpoint.UploadAsync(endpoint, auth, this.FileToUpload, true);
                    isAuthRequired = authError != null;
                    isValid = !isAuthRequired;
                }
                catch
                {
                    authError = null;
                    isAuthRequired = false;
                    isValid = false;
                }

                await this.Dispatcher.InvokeAsync(() =>
                {
                    if (isValid)
                    {
                        var index = this.RecentEndpoints.IndexOf(endpoint);
                        if (index == -1)
                        {
                            this.RecentEndpoints.Insert(0, endpoint);
                        }
                        else if (index != 0)
                        {
                            this.RecentEndpoints.Move(index, 0);
                        }

                        while (this.RecentEndpoints.Count > 25)
                        {
                            this.RecentEndpoints.RemoveAt(25);
                        }

                        WindowsRegistry.StoreRecentEndpoints(this.RecentEndpoints.ToArray());
                    }

                    if (auth != null && authError != null)
                    {
                        this.FailureMessage = new Exception(authError);
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FailureMessage)));
                    }
                    if (this.IsAuthRequired != isAuthRequired)
                    {
                        this.IsAuthRequired = isAuthRequired;
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAuthRequired)));
                    }
                    if (this.IsFeedValid != isValid)
                    {
                        this.IsFeedValid = isValid;
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFeedValid)));
                    }
                    this.isUpdating = false;
                    if (this.updateQueued)
                    {
                        this.updateQueued = false;
                        this.CheckAuth();
                    }
                });
            });
        }

        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            this.credentials = new NetworkCredential(this.username.Text, this.password.SecurePassword);

            this.CheckAuth();
        }

        private bool waitingForUpload = false;

        private void DismissError_Click(object sender, RoutedEventArgs e)
        {
            this.FailureMessage = null;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FailureMessage)));
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!this.waitingForUpload)
            {
                this.Close();
            }
        }

        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            if (this.waitingForUpload || this.isUpdating || this.updateQueued)
            {
                return;
            }

            this.waitingForUpload = true;

            var endpoint = this.EndpointUri;
            var auth = this.credentials;

            Task.Run(async () =>
            {
                try
                {
                    await UniversalPackageEndpoint.UploadAsync(endpoint, auth, this.FileToUpload);
                }
                catch (Exception ex)
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        this.FailureMessage = ex;
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FailureMessage)));
                        this.waitingForUpload = false;
                    });
                    return;
                }

                await this.Dispatcher.InvokeAsync(() =>
                {
                    this.Close();
                });
            });
        }
    }
}
