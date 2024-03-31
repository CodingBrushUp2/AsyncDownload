using AsyncDownload.Interfaces;
using AsyncDownload.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace AsyncDownload.Test;

public class DownloadServiceTests
{
    private readonly Mock<IFileService> _fileServiceMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<DownloadService>> _loggerMock = new();
    private readonly DownloadService _downloadService;

    private readonly List<string> _urls = new()
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
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        _downloadService = new DownloadService(_httpClientFactoryMock.Object, _fileServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DownloadFileAsync_ShouldDownloadFile_WhenUrlIsInvalid()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _downloadService.CheckAndDownloadUrlsAsync(_urls, cancellationToken);

        // Assert
        _fileServiceMock.Verify(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken), Times.Exactly(3));
    }

    [Fact]
    public async Task LogsError_WhenHttpRequestFails()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _downloadService.CheckAndDownloadUrlsAsync(new[] { "https://dddddddsssds.com" }, cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully downloaded and saved")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(1));
    }

    [Fact]
    public async Task LogsError_WhenFileWriteFails()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _fileServiceMock.Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken))
                       .ThrowsAsync(new Exception("File write failed"));

        // Act
        await _downloadService.CheckAndDownloadUrlsAsync(_urls, cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains("Error checking or downloading URL") &&
                    v.ToString().Contains("Error: File write failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task LogsError_WhenHttpRequestThrowsException()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network failure"));

        var client = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        // Act
        await _downloadService.CheckAndDownloadUrlsAsync(_urls, cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Network failure")),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(_urls.Count)); // Assuming you want to log an error for each URL in the event of a network failure
    }

    [Fact]
    public async Task DoesNotAttemptDownload_WhenUrlListIsEmpty()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _downloadService.CheckAndDownloadUrlsAsync(new List<string>(), cancellationToken);

        // Assert
        _fileServiceMock.Verify(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), cancellationToken), Times.Never);
    }
}
