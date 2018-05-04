using Inedo.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        internal static async Task<string> UploadAsync(string endpointUri, NetworkCredential credentials, Stream stream, bool dryRun = false)
        {
            endpointUri = endpointUri.TrimEnd('/');

            using (var client = new HttpClient(new HttpClientHandler
            {
                UseDefaultCredentials = credentials == null,
                Credentials = credentials
            })
            {
                Timeout = dryRun ? TimeSpan.FromSeconds(10) : Timeout.InfiniteTimeSpan
            })
            using (var request = new HttpRequestMessage(HttpMethod.Put, $"{endpointUri}/upload"))
            {
                if (dryRun)
                {
                    request.Content = new ByteArrayContent(new byte[0])
                    {
                        Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/zip")
                        }
                    };
                }
                else
                {
                    request.Content = new StreamContent(new UndisposableStream(stream))
                    {
                        Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/zip")
                        }
                    };
                }

                using (var cts = new CancellationTokenSource(client.Timeout))
                using (var response = await client.SendAsync(request, cts.Token))
                {
                    if (dryRun)
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                            return await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                            return null;

                        response.EnsureSuccessStatusCode();

                        throw new ArgumentException("Invalid upack endpoint.", nameof(endpointUri));
                    }

                    response.EnsureSuccessStatusCode();

                    return null;
                }
            }
        }

        private sealed class DelayForeverStream : Stream
        {
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => 0; set => throw new NotSupportedException(); }
            public override bool CanTimeout => true;
            public override int ReadTimeout { get; set; }

            public DelayForeverStream()
            {
                this.ReadTimeout = Timeout.Infinite;
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                Thread.Sleep(this.ReadTimeout);
                throw new TimeoutException();
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                return this.ReadAsync(buffer, offset, count);
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                ((Task)asyncResult).Wait();
                throw new TimeoutException();
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                using (var cts1 = new CancellationTokenSource(this.ReadTimeout))
                using (var cts2 = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cancellationToken))
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cts2.Token);
                }
                return 0;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
