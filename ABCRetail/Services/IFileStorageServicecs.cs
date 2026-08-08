namespace ABCRetail.Services
{
    public interface IFileStorageService
    {
        Task WriteLogAsync(string logMessage);
        Task<List<string>> GetLogFilesAsync();
        Task<string> ReadLogAsync(string fileName);
    }
}
