using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetail.functions
{
    public class UploadProductImageFunction
    {
        private readonly ILogger<UploadProductImageFunction> _logger;

        public UploadProductImageFunction(
            ILogger<UploadProductImageFunction> logger)
        {
            _logger = logger;
        }

        [Function("UploadProductImage")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post")] HttpRequest req)
        {
            _logger.LogInformation(
                "UploadProductImage Azure Function started.");

            try
            {
                // Make sure a file was sent
                if (!req.HasFormContentType)
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "The request must contain form data."
                    });
                }

                IFormCollection form =
                    await req.ReadFormAsync();

                IFormFile? file =
                    form.Files.GetFile("file");

                if (file == null || file.Length == 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "No image was provided."
                    });
                }

                // Get the ABC Retail storage connection string
                string? connectionString =
                    Environment.GetEnvironmentVariable(
                        "ABCRetailStorage");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogError(
                        "ABCRetailStorage connection string is missing.");

                    return new ObjectResult(new
                    {
                        success = false,
                        message = "Storage configuration is missing."
                    })
                    {
                        StatusCode =
                            StatusCodes.Status500InternalServerError
                    };
                }

                // Connect to the ABC Retail product-images container
                BlobContainerClient containerClient =
                    new BlobContainerClient(
                        connectionString,
                        "product-images");

                await containerClient.CreateIfNotExistsAsync();

                // Get a safe version of the original filename
                string originalFileName =
                    Path.GetFileName(file.FileName);

                // Create a unique blob filename
                string blobName =
                    $"{Guid.NewGuid()}-{originalFileName}";

                BlobClient blobClient =
                    containerClient.GetBlobClient(blobName);

                // Upload the image
                using Stream stream =
                    file.OpenReadStream();

                await blobClient.UploadAsync(
                    stream,
                    new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    });

                _logger.LogInformation(
                    "Product image {BlobName} uploaded successfully.",
                    blobName);

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Product image uploaded successfully.",
                    blobName = blobName,
                    blobUrl = blobClient.Uri.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while uploading the product image.");

                return new ObjectResult(new
                {
                    success = false,
                    message = "Failed to upload product image.",
                    error = ex.Message
                })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}