AsyncDownload
=============

About The Project
-----------------

`AsyncDownload` is a .NET 8 based application designed for asynchronous downloading of content from the Internet. Utilizing the power of .NET's asynchronous programming model, it provides an efficient means of downloading content from specified URLs concurrently, without blocking the user interface or the application's main execution thread.

Architecture Overview
---------------------

The application adopts a modular architecture focusing on separation of concerns, making it scalable and maintainable. Key components include:

### Core Components

-   DownloadService: Orchestrates the download process, managing asynchronous calls to fetch content from URLs and save them to the filesystem. It utilizes `IHttpClientFactory` for resilient HTTP requests and employs a `SemaphoreSlim` for controlling concurrency, ensuring a customizable limit on the number of simultaneous downloads.

-   FileService (Implements `IFileService`): Handles file operations, specifically writing downloaded content to the filesystem asynchronously. This abstraction allows for easier unit testing and future extensions, such as adding support for different storage backends.

-   Program: Sets up dependency injection, logging, and application configuration. It initializes `DownloadService` with required dependencies and triggers the download process.

### External Libraries and Frameworks

-   Microsoft.Extensions.DependencyInjection: Used for implementing dependency injection, allowing for decoupled and testable code.

-   Microsoft.Extensions.Logging: Provides logging capabilities, essential for monitoring the application's operation and troubleshooting issues.

-   Polly: Integrated with `IHttpClientFactory` for resilience and transient fault handling. It's configured to retry HTTP requests based on certain policies, enhancing the robustness of web requests.

### Error Handling and Logging

Comprehensive error handling is implemented, with graceful handling of exceptions such as network failures or file write errors. Logging is extensively used across components, providing insights into the application's behavior and any errors that occur.

Getting Started
---------------

### Prerequisites

-   .NET 8 SDK

### Installation and Running

1.  Clone the repository: `git clone https://github.com/CodingBrushUp2/AsyncDownload.git`
2.  Build the solution: `dotnet build`
3.  Run the application: `dotnet run`

Key Functionalities
-------------------

-   Asynchronous content downloading from a list of URLs.
-   Configurable concurrency limit for simultaneous downloads.
-   Resilient HTTP requests with retry policies.
-   Detailed logging for operational monitoring and error diagnosis.

Testing
-------

Unit tests are provided for critical components, using `XUnit`, `Moq` for mocking dependencies, and asserting outcomes with precise conditions. The tests cover scenarios such as successful downloads, handling of HTTP request failures, and file write errors, ensuring the reliability and robustness of the service.

### Key Test Cases

-   `DownloadFileAsync_ShouldDownloadFile_WhenUrlIsInvalid`: Verifies download functionality with invalid URLs.
-   `LogsError_WhenFileWriteFails`: Tests error logging when file writing fails.
-   `DoesNotAttemptDownload_WhenUrlListIsEmpty`: Ensures no download attempts are made when the URL list is empty.

Contributing
------------

Contributions are welcome. Please feel free to fork the repository, make changes, and submit pull requests.

License
-------

Distributed under the MIT License.
