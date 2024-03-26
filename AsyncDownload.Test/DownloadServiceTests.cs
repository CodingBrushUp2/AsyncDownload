using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncDownload.Interfaces;
using AsyncDownload.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AsyncDownload.Test;

public class DownloadServiceTests
{
    private readonly Mock<IFileService> fileServiceMock = new();
    private readonly Mock<IHttpClientFactory> httpClientFactoryMock = new();
    private readonly Mock<ILogger<DownloadService>> loggerMock = new();
    private DownloadService downloadService;

    private readonly List<string> urls = new()
    {
        "https://google.com",
        "https://dddddddsssds.com",
        "https://linkedin.com"
    };

    public DownloadServiceTests()
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsoluteUri == "https://dddddddsssds.com"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)) // Simulating a NotFound response
            .Verifiable();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.AbsoluteUri != "https://dddddddsssds.com"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes("Hello, World!"))
            }) // Simulating OK response for other URLs
            .Verifiable();

        var client = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com") };
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        downloadService = new DownloadService(httpClientFactoryMock.Object, fileServiceMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task DownloadFileAsync_ShouldDownloadFile_WhenUrlIsInvalid()
    {
        // Act
        await downloadService.CheckAndDownloadUrlsAsync(urls);

        // Assert
        fileServiceMock.Verify(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task LogsError_WhenHttpRequestFails()
    {
        // Act
        await downloadService.CheckAndDownloadUrlsAsync(new[] { "https://dddddddsssds.com" });

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully downloaded and saved")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(1));
    }

    [Fact]
    public async Task LogsError_WhenFileWriteFails()
    {
        // Arrange
        fileServiceMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                       .ThrowsAsync(new Exception("File write failed"));

        // Act
        await downloadService.CheckAndDownloadUrlsAsync(urls);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                            v.ToString().Contains("Error checking or downloading URL") && 
                            v.ToString().Contains("Error: File write failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task LogsError_WhenHttpRequestThrowsException()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network failure"));

        var client = new HttpClient(mockHandler.Object);
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        // Act
        await downloadService.CheckAndDownloadUrlsAsync(urls);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Network failure")),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(urls.Count)); // Assuming you want to log an error for each URL in the event of a network failure
    }

    [Fact]
    public async Task DoesNotAttemptDownload_WhenUrlListIsEmpty()
    {
        // Act
        await downloadService.CheckAndDownloadUrlsAsync(new List<string>());

        // Assert
        fileServiceMock.Verify(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

}
