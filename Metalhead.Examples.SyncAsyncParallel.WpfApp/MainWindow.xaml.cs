using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

using Metalhead.Examples.SyncAsyncParallel.Core;
using Metalhead.Examples.SyncAsyncParallel.Core.Models;

namespace Metalhead.Examples.SyncAsyncParallel.WpfApp;

public partial class MainWindow : Window
{
    private CancellationTokenSource _cts = new();
    private readonly Progress<ProgressReport> _progress;
    private readonly List<string> _urlsToDownload;

    public MainWindow()
    {
        InitializeComponent();
        _progress = new();
        _progress.ProgressChanged += ReportProgress;
        cancelAsyncOperationButton.IsEnabled = false;
        _urlsToDownload = GetUrlsToDownload();
    }

    private static List<string> GetUrlsToDownload()
    {
        string json = File.ReadAllText("Urls.json");
        var urlsToDownload = JsonSerializer.Deserialize<UrlsToDownload>(json);
        return urlsToDownload is null ? ([]) : urlsToDownload.Urls;
    }

    private int MaxDegreeOfParallelism
    {
        get
        {
            var valid = int.TryParse(maxParallelismTextBox.Text, out int result);

            if (!valid || result < -1 || result == 0)
            {
                maxParallelismTextBox.Text = "-1";
                return -1;
            }
            else
            {
                return result;
            }
        }
    }

    private void ReportProgress(object? sender, ProgressReport e)
    {
        resultsProgressBar.Value = e.PercentageComplete;
        DisplayResult(e.Download);
    }

    private void DisplayResults(List<Download> downloads)
    {
        foreach (var result in downloads)
        {
            DisplayResult(result);
        }
    }

    private void DisplayResult(Download download)
    {
        var size = download.SizeBytes is null ? "Error fetching URL" : $"{download.SizeBytes} bytes";
        resultsTextBlock.Text += $"{download.Url} ({size}){Environment.NewLine}";
    }

    private void ClearResults()
    {
        resultsProgressBar.Value = 0;
        resultsTextBlock.Text = string.Empty;
    }

    private void EnableDownloadButtons()
    {
        syncButton.IsEnabled = true;
        parallelSyncButton.IsEnabled = true;
        asyncButton.IsEnabled = true;
        parallelAsyncButton.IsEnabled = true;
        maxParellelismStackPanel.IsEnabled = true;
    }

    private void DisableDownloadButtons()
    {
        syncButton.IsEnabled = false;
        parallelSyncButton.IsEnabled = false;
        asyncButton.IsEnabled = false;
        parallelAsyncButton.IsEnabled = false;
        maxParellelismStackPanel.IsEnabled = false;
    }

    private void Sync_Click(object sender, RoutedEventArgs e)
    {
        ClearResults();
        DisableDownloadButtons();
        // Force update of UI on this non-asynchronous method to show buttons as disabled.
        Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
        var stopWatch = Stopwatch.StartNew();

        try
        {
            var results = Helper.GetDownloadsSync(_urlsToDownload);
            DisplayResults(results);
        }
        finally
        {
            resultsProgressBar.Value = 100;
            EnableDownloadButtons();
        }

        stopWatch.Stop();
        resultsTextBlock.Text += $"Elapsed millieseconds: {stopWatch.ElapsedMilliseconds}";
    }    

    private void ParallelSync_Click(object sender, RoutedEventArgs e)
    {
        ClearResults();
        DisableDownloadButtons();
        var maxDegreeOfParallelism = MaxDegreeOfParallelism;
        // Force update of UI on this non-asynchronous method to show buttons as disabled.
        Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
        var stopWatch = Stopwatch.StartNew();

        try
        {
            var results = Helper.GetDownloadsParallelSync(_urlsToDownload, maxDegreeOfParallelism);
            DisplayResults(results);
        }
        finally
        {
            resultsProgressBar.Value = 100;
            EnableDownloadButtons();
        }

        stopWatch.Stop();
        resultsTextBlock.Text += $"Elapsed millieseconds: {stopWatch.ElapsedMilliseconds}";
    }

    private async void Async_Click(object sender, RoutedEventArgs e)
    {
        ClearResults();
        DisableDownloadButtons();
        cancelAsyncOperationButton.IsEnabled = true;
        var stopWatch = Stopwatch.StartNew();

        try
        {
            await Helper.GetDownloadsAsync(_urlsToDownload, _progress, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            resultsTextBlock.Text += $"Asychronous operation was cancelled{Environment.NewLine}";
        }
        finally
        {
            cancelAsyncOperationButton.IsEnabled = false;
            EnableDownloadButtons();            
        }

        stopWatch.Stop();
        resultsTextBlock.Text += $"Elapsed millieseconds: {stopWatch.ElapsedMilliseconds}";
    }

    private async void ParallelAsync_Click(object sender, RoutedEventArgs e)
    {
        ClearResults();
        DisableDownloadButtons();
        cancelAsyncOperationButton.IsEnabled = true;
        var stopWatch = Stopwatch.StartNew();

        try
        {
            // Cancellation is only possible if MaxDegreeOfParallelism is less than the amount of downloads.
            await Helper.GetDownloadsParallelAsync(_urlsToDownload, _progress, MaxDegreeOfParallelism, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            resultsTextBlock.Text += $"Asychronous operation was cancelled{Environment.NewLine}";
        }
        finally
        {
            cancelAsyncOperationButton.IsEnabled = false;
            EnableDownloadButtons();
        }

        stopWatch.Stop();
        resultsTextBlock.Text += $"Elapsed millieseconds: {stopWatch.ElapsedMilliseconds}";
    }

    private void CancelOperation_Click(object sender, RoutedEventArgs e)
    {
        cancelAsyncOperationButton.IsEnabled = false;
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
    }
}