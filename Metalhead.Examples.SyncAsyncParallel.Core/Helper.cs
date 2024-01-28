using System.Collections.Concurrent;
using System.Text;

using Metalhead.Examples.SyncAsyncParallel.Core.Models;

namespace Metalhead.Examples.SyncAsyncParallel.Core
{
    public static class Helper
    {
        public static List<Download> GetDownloadsSync(List<string> urls)
        {
            List<Download> downloads = [];

            foreach (string url in urls)
            {
                downloads.Add(GetDownload(url));
            }

            return downloads;
        }

        public static List<Download> GetDownloadsParallelSync(List<string> urls, int maxDegreeOfParallelism)
        {
            List<Download> downloads = [];

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            Parallel.ForEach<string>(urls, parallelOptions, (url) =>
            {
                downloads.Add(GetDownload(url));
            });

            return downloads;
        }

        public static async Task<List<Download>> GetDownloadsAsync(List<string> urls, IProgress<ProgressReport> progress, CancellationToken cancellationToken)
        {
            List<Download> downloads = [];
            ProgressReport report = new();

            foreach (string url in urls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var download = await GetDownloadAsync(url).ConfigureAwait(false);
                downloads.Add(download);

                report.Download = download;
                report.PercentageComplete = downloads.Count * 100 / urls.Count;
                progress.Report(report);
            }

            return downloads;
        }

        public static async Task<ConcurrentBag<Download>> GetDownloadsParallelAsync(List<string> urls, IProgress<ProgressReport> progress, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            ConcurrentBag<Download> downloads = [];

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            await Parallel.ForEachAsync<string>(urls, parallelOptions, async (url, _) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var download = await GetDownloadAsync(url).ConfigureAwait(false);
                downloads.Add(download);

                // Create new instance of ProgressReport for each iteration to avoid overwriting issues caused by multithreading.
                ProgressReport report = new();
                report.Download = download;
                report.PercentageComplete = downloads.Count * 100 / urls.Count;
                progress.Report(report);
            }).ConfigureAwait(false);

            return downloads;
        }

        private static Download GetDownload(string url)
        {
            Download download = new();
            download.Url = url;

            using (HttpClient client = new())
            {                
                try
                {
                    var response = client.GetAsync(url).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        download.SizeBytes = Encoding.UTF8.GetBytes(content).Length;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException or HttpRequestException or TaskCanceledException)
                    {
                        if (ex is AggregateException aggregateException)
                        {
                            if (aggregateException.Flatten().InnerExceptions.Any(e => e is not HttpRequestException))
                            {
                                throw;
                            }
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return download;
        }

        private static async Task<Download> GetDownloadAsync(string url)
        {
            Download download = new();
            download.Url = url;

            using (HttpClient client = new())
            {                
                try
                {
                    var response = await client.GetAsync(url).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStringAsync().Result;
                        download.SizeBytes = Encoding.UTF8.GetBytes(content).Length;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException or HttpRequestException or TaskCanceledException)
                    {
                        if (ex is AggregateException aggregateException)
                        {
                            if (aggregateException.Flatten().InnerExceptions.Any(e => e is not HttpRequestException))
                            {
                                throw;
                            }
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return download;
        }
    }
}
