using AsyncDownload.Interfaces;
using AsyncDownload.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

var services = new ServiceCollection();
ConfigureServices(services);
var serviceProvider = services.BuildServiceProvider();
var downloadService = serviceProvider.GetRequiredService<DownloadService>();

var urls = new List<string>
        {
            "https://google.com",
            "https://dddddddsssds.com",
            "https://linkedin.com",
        };

await downloadService.CheckAndDownloadUrlsAsync(urls);

Console.WriteLine("Download process completed.");

static void ConfigureServices(IServiceCollection services)
{
    services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information));

    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError() // Handles HttpRequestException, 5XX and 408 statuses
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
                //logger.LogWarning($"Retrying due to: {outcome.Exception}. Waiting {timespan} before retry. Attempt {retryAttempt}.");
                logger.LogWarning($"Retrying. Waiting {timespan} before retry. Attempt {retryAttempt}.");
            });

    services
        .AddHttpClient("DownloadClient")
        .AddPolicyHandler(retryPolicy);

    services.AddSingleton<IFileService, FileService>();
    services.AddSingleton<DownloadService>();
}
