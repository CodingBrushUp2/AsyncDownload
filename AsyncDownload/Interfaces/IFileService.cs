namespace AsyncDownload.Interfaces;

public interface IFileService
{
    Task WriteAllTextAsync(string path, string contents);
}
