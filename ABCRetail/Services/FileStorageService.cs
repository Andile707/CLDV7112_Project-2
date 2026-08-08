using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.Text;

namespace ABCRetail.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly ShareClient _shareClient;

        public FileStorageService(IConfiguration configuration)
        {
            string connectionString =
                configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");

            _shareClient = new ShareClient(
                connectionString,
                "logs");
        }

        public async Task WriteLogAsync(string logMessage)
        {
            await _shareClient.CreateIfNotExistsAsync();

            ShareDirectoryClient rootDirectory =
                _shareClient.GetRootDirectoryClient();

            string fileName =
                     $"log-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.txt";

            ShareFileClient fileClient =
                rootDirectory.GetFileClient(fileName);

            string content =
                    $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC: {logMessage}";

            byte[] bytes = Encoding.UTF8.GetBytes(content);

            await fileClient.CreateAsync(bytes.Length);

            using MemoryStream stream = new MemoryStream(bytes);

            await fileClient.UploadRangeAsync(
                new Azure.HttpRange(0, bytes.Length),
                stream);
        }

        public async Task<List<string>> GetLogFilesAsync()
        {
            await _shareClient.CreateIfNotExistsAsync();

            ShareDirectoryClient rootDirectory =
                _shareClient.GetRootDirectoryClient();

            List<string> files = new List<string>();

            await foreach (
                ShareFileItem item in
                rootDirectory.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    files.Add(item.Name);
                }
            }

            return files;
        }

        public async Task<string> ReadLogAsync(string fileName)
        {
            ShareDirectoryClient rootDirectory =
                _shareClient.GetRootDirectoryClient();

            ShareFileClient fileClient =
                rootDirectory.GetFileClient(fileName);

            ShareFileDownloadInfo download =
                await fileClient.DownloadAsync();

            using StreamReader reader =
                new StreamReader(download.Content);

            return await reader.ReadToEndAsync();
        }
    }
}
