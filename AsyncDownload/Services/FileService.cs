using AsyncDownload.Interfaces;

namespace AsyncDownload.Services;

public class FileService : IFileService
{
    public async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(path, contents, cancellationToken);
    }
}
