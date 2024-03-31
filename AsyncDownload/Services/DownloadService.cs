using AsyncDownload.Interfaces;
using Microsoft.Extensions.Logging;

namespace AsyncDownload.Services;

public class DownloadService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileService _fileService;
    private readonly ILogger<DownloadService> _logger;
    private readonly string _outputDirectory = "DownloadedPages";
    private readonly SemaphoreSlim _semaphoreSlim = new(5);

    public DownloadService(IHttpClientFactory httpClientFactory, IFileService fileService, ILogger<DownloadService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _fileService = fileService;
        _logger = logger;
        Directory.CreateDirectory(_outputDirectory);
    }

    public async Task CheckAndDownloadUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(urls);
        var client = _httpClientFactory.CreateClient("DownloadClient");

        _logger.LogInformation("Starting to check and download URLs...");

        var tasks = urls.Select(url => CheckAndDownloadUrlAsync(client, url, cancellationToken)).ToList();
        await Task.WhenAll(tasks);

        _logger.LogInformation("Completed checking and downloading URLs.");
    }

    private async Task CheckAndDownloadUrlAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation($"Checking URL: {url}");
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"URL exists and is being downloaded: {url}");
                await DownloadWebsiteAsync(url, response, cancellationToken);
            }
            else
            {
                _logger.LogWarning($"URL check failed: {url} with status code {response.StatusCode}");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Error checking or downloading URL: {url}, Error: {ex.Message}");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private async Task DownloadWebsiteAsync(string url, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_outputDirectory, GetSafeFileName(url));

        //var content = await response.Content.ReadAsStringAsync(cancellationToken);
        //await _fileService.WriteAllTextAsync(filePath, content, cancellationToken);
        //or using FileStream
        using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
            _logger.LogInformation($"Successfully downloaded and saved: {url}");
        }
        _logger.LogInformation($"Successfully downloaded and saved: {url}");
    }

    private static string GetSafeFileName(string url)
    {
        return url.Replace("http://", "").Replace("https://", "").Replace("/", "_") + ".html";
    }
}
