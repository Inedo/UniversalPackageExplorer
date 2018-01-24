using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using UniversalPackageExplorer.UPack;

namespace UniversalPackageExplorer
{
    /// <summary>
    /// Interaction logic for OpenFromFeedWindow.xaml
    /// </summary>
    public partial class OpenFromFeedWindow : Window, INotifyPropertyChanged
    {
        public OpenFromFeedWindow()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string endpointUri = "https://proget.example.com/upack/FeedName";
        public string EndpointUri
        {
            get => this.endpointUri;
            set
            {
                this.endpointUri = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndpointUri)));
                this.credentials = null;
                this.IsAuthRequired = false;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAuthRequired)));

                this.UpdateSearch();
            }
        }

        private NetworkCredential credentials;

        private string searchText = string.Empty;
        public string SearchText
        {
            get => this.searchText;
            set
            {
                this.searchText = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                this.UpdateSearch();
            }
        }

        public bool IsAuthRequired { get; private set; }
        public bool IsFeedValid { get; private set; }
        public IList<UniversalPackageInfo> SearchResults { get; private set; }

        private bool isUpdating = false;

        private void UpdateSearch()
        {
            if (this.isUpdating)
            {
                return;
            }

            this.isUpdating = true;

            Task.Run(async () =>
            {
                IList<UniversalPackageInfo> searchResults;
                bool isAuthRequired;
                bool isValid = true;
                try
                {
                    (searchResults, isAuthRequired) = await UniversalPackageEndpoint.SearchAsync(this.EndpointUri, this.SearchText, this.credentials);
                    isValid = !isAuthRequired;
                }
                catch
                {
                    searchResults = null;
                    isAuthRequired = false;
                    isValid = false;
                }

                await this.Dispatcher.InvokeAsync(() =>
                {
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
                    this.SearchResults = searchResults;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchResults)));
                    this.isUpdating = false;
                });
            });
        }

        public async Task<UniversalPackage> DownloadAsync()
        {
            var info = await this.Dispatcher.InvokeAsync(() => (UniversalPackageInfo)this.grid.SelectedItem);
            return await UniversalPackageEndpoint.DownloadAsync(this.EndpointUri, info.GroupAndName, info.Version, this.credentials);
        }

        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            this.credentials = new NetworkCredential(this.username.Text, this.password.SecurePassword);

            this.UpdateSearch();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;

            this.Close();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            this.Close();
        }
    }
}
