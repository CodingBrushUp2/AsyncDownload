using AsyncDownload.Interfaces;

namespace AsyncDownload.Services;

public class FileService : IFileService
{
    public async Task WriteAllTextAsync(string path, string contents)
    {
        await File.WriteAllTextAsync(path, contents);
    }
}
