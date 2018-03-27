using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

            if (this.RecentEndpoints.Any())
            {
                this.UpdateSearch();
            }
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
        private bool updateQueued = false;

        private void UpdateSearch()
        {
            if (this.isUpdating)
            {
                this.updateQueued = true;
                return;
            }

            this.isUpdating = true;

            var endpoint = this.EndpointUri;
            var search = this.SearchText;
            var auth = this.credentials;

            Task.Run(async () =>
            {
                IList<UniversalPackageInfo> searchResults;
                bool isAuthRequired;
                bool isValid = true;
                try
                {
                    (searchResults, isAuthRequired) = await UniversalPackageEndpoint.SearchAsync(endpoint, search, auth);
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
                    if (this.updateQueued)
                    {
                        this.updateQueued = false;
                        this.UpdateSearch();
                    }
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
