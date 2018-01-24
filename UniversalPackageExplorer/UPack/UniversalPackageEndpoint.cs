using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalPackageExplorer.UPack
{
    public sealed class UniversalPackageEndpoint
    {
        public static async Task<(IList<UniversalPackageInfo> searchResults, bool isAuthRequired)> SearchAsync(string endpointUri, string searchText, NetworkCredential credentials = null)
        {
            endpointUri = endpointUri.TrimEnd('/');

            using (var client = new HttpClient(new HttpClientHandler
            {
                UseDefaultCredentials = credentials == null,
                Credentials = credentials
            })
            {
                Timeout = Timeout.InfiniteTimeSpan
            })
            {
                using (var response = await client.GetAsync(string.IsNullOrWhiteSpace(searchText) ? $"{endpointUri}/packages" : $"{endpointUri}/search?term={Uri.EscapeDataString(searchText)}"))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return (null, true);
                    }

                    response.EnsureSuccessStatusCode();

                    return (JsonConvert.DeserializeObject<UniversalPackageInfo[]>(await response.Content.ReadAsStringAsync()), false);
                }
            }
        }

        internal static async Task<UniversalPackage> DownloadAsync(string endpointUri, string groupAndName, string version, NetworkCredential credentials)
        {
            endpointUri = endpointUri.TrimEnd('/');

            using (var client = new HttpClient(new HttpClientHandler
            {
                UseDefaultCredentials = credentials == null,
                Credentials = credentials
            })
            {
                Timeout = Timeout.InfiniteTimeSpan
            })
            {
                using (var response = await client.GetAsync($"{endpointUri}/download/{groupAndName}/{Uri.EscapeDataString(version)}", HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    return await UniversalPackage.CreateAsync(await response.Content.ReadAsStreamAsync());
                }
            }
        }
    }
}
