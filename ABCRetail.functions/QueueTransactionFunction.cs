using ABCRetail.functions.Models;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ABCRetail.functions
{
    public class QueueTransactionFunction
    {
        private readonly ILogger<QueueTransactionFunction> _logger;

        public QueueTransactionFunction(
            ILogger<QueueTransactionFunction> logger)
        {
            _logger = logger;
        }

        [Function("QueueTransaction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post")] HttpRequest req)
        {
            _logger.LogInformation(
                "QueueTransaction Azure Function started.");

            try
            {
                OrderFunctionModel? order =
                    await JsonSerializer.DeserializeAsync<OrderFunctionModel>(
                        req.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (order == null)
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "Order information is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(order.OrderNumber) ||
                    string.IsNullOrWhiteSpace(order.CustomerName) ||
                    string.IsNullOrWhiteSpace(order.ProductName) ||
                    order.Quantity <= 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        success = false,
                        message = "Valid order information is required."
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

                // Connect to the ABC Retail Queue Storage
                QueueClient queueClient =
                    new QueueClient(
                        connectionString,
                        "order-inventory-queue");

                await queueClient.CreateIfNotExistsAsync();

                // Transaction message
                string orderMessage =
                    $"Processing Order {order.OrderNumber} - " +
                    $"Customer: {order.CustomerName}, " +
                    $"Product: {order.ProductName}, " +
                    $"Quantity: {order.Quantity}";

                // Inventory message
                string inventoryMessage =
                    $"Inventory update - Product: " +
                    $"{order.ProductName}, " +
                    $"Quantity ordered: {order.Quantity}";

                // Write both messages to Azure Queue Storage
                await queueClient.SendMessageAsync(
                    orderMessage);

                await queueClient.SendMessageAsync(
                    inventoryMessage);

                _logger.LogInformation(
                    "Order {OrderNumber} and inventory messages " +
                    "were successfully written to Azure Queue Storage.",
                    order.OrderNumber);

                return new OkObjectResult(new
                {
                    success = true,
                    message =
                        "Order and inventory messages queued successfully.",
                    orderNumber = order.OrderNumber
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid JSON received by QueueTransaction.");

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
                    "An error occurred while writing to Azure Queue Storage.");

                return new ObjectResult(new
                {
                    success = false,
                    message =
                        "Failed to send transaction information to the queue.",
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