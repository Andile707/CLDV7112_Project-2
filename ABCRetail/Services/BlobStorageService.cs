using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ABCRetail.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private const string ContainerName = "product-images";

        private readonly BlobContainerClient _containerClient;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

        public BlobStorageService(IConfiguration configuration)
        {
            string connectionString =
                configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "The Azure Storage connection string is missing.");

            _containerClient = new BlobContainerClient(
                connectionString,
                ContainerName);
        }

        public async Task<string> UploadImageAsync(
            IFormFile imageFile)
        {
            ArgumentNullException.ThrowIfNull(imageFile);

            if (imageFile.Length == 0)
            {
                throw new ArgumentException(
                    "The selected image is empty.",
                    nameof(imageFile));
            }

            const long maximumFileSize = 5 * 1024 * 1024;

            if (imageFile.Length > maximumFileSize)
            {
                throw new ArgumentException(
                    "The image cannot be larger than 5 MB.",
                    nameof(imageFile));
            }

            string extension =
                Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                throw new ArgumentException(
                    "Only JPG, JPEG, PNG, GIF and WEBP images are allowed.",
                    nameof(imageFile));
            }

            await _containerClient.CreateIfNotExistsAsync(
                PublicAccessType.Blob);

            string safeFileName =
                Path.GetFileNameWithoutExtension(imageFile.FileName);

            string blobName =
                $"{Guid.NewGuid()}-{safeFileName}{extension}";

            BlobClient blobClient =
                _containerClient.GetBlobClient(blobName);

            await using Stream stream = imageFile.OpenReadStream();

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = imageFile.ContentType
                }
            };

            await blobClient.UploadAsync(
                stream,
                uploadOptions);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            if (!Uri.TryCreate(
                    imageUrl,
                    UriKind.Absolute,
                    out Uri? imageUri))
            {
                return;
            }

            string blobName =
                Uri.UnescapeDataString(
                    Path.GetFileName(imageUri.LocalPath));

            BlobClient blobClient =
                _containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots);
        }
    }
}
