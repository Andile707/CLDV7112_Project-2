using ABCRetail.functions.Models;
using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ABCRetail.functions
{
    public class WriteLogFunction
    {
        private readonly ILogger<WriteLogFunction> _logger;

        public WriteLogFunction(
            ILogger<WriteLogFunction> logger)
        {
            _logger = logger;
        }

        [Function("WriteLog")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post")] HttpRequest req)
        {
            _logger.LogInformation(
                "WriteLog Azure Function started.");

            try
            {
                LogFunctionModel? logRequest =
                    await JsonSerializer.DeserializeAsync<LogFunctionModel>(
                        req.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (logRequest == null ||
                    string.IsNullOrWhiteSpace(logRequest.Message))
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "A log message is required."
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

                // Connect to the ABC Retail Azure File Share
                ShareClient shareClient =
                    new ShareClient(
                        connectionString,
                        "logs");

                await shareClient.CreateIfNotExistsAsync();

                ShareDirectoryClient rootDirectory =
                    shareClient.GetRootDirectoryClient();

                // Create a unique log file name
                string fileName =
                    $"log-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.txt";

                ShareFileClient fileClient =
                    rootDirectory.GetFileClient(fileName);

                // Create the log content
                string content =
                    $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC: " +
                    $"{logRequest.Message}";

                byte[] bytes =
                    Encoding.UTF8.GetBytes(content);

                // Create the file in Azure Files
                await fileClient.CreateAsync(bytes.Length);

                using MemoryStream stream =
                    new MemoryStream(bytes);

                // Upload the log content
                await fileClient.UploadRangeAsync(
                    new HttpRange(0, bytes.Length),
                    stream);

                _logger.LogInformation(
                    "Log file {FileName} created successfully.",
                    fileName);

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Log file created successfully.",
                    fileName = fileName
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid JSON received by WriteLog.");

                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Invalid JSON format."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while writing the log file.");

                return new ObjectResult(new
                {
                    success = false,
                    message = "Failed to create log file.",
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