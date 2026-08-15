using Azure.Data.Tables;
using ABCRetail.functions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ABCRetail.functions
{
    public class StoreCustomerFunction
    {
        private readonly ILogger<StoreCustomerFunction> _logger;

        public StoreCustomerFunction(
            ILogger<StoreCustomerFunction> logger)
        {
            _logger = logger;
        }

        [Function("StoreCustomer")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post")] HttpRequest req)
        {
            _logger.LogInformation(
                "StoreCustomer Azure Function started.");

            try
            {
                // Read customer JSON sent to the function
                CustomerProfileFunctionModel? customer =
                    await JsonSerializer.DeserializeAsync<CustomerProfileFunctionModel>(
                        req.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (customer == null)
                {
                    return new BadRequestObjectResult(
                        "Customer information is required.");
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(customer.FirstName) ||
                    string.IsNullOrWhiteSpace(customer.LastName) ||
                    string.IsNullOrWhiteSpace(customer.Email))
                {
                    return new BadRequestObjectResult(
                        "First name, last name and email are required.");
                }

                // Get the ABC Retail storage connection string
                string? connectionString =
                    Environment.GetEnvironmentVariable(
                        "ABCRetailStorage");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogError(
                        "ABCRetailStorage connection string is missing.");

                    return new ObjectResult(
                        "Storage configuration is missing.")
                    {
                        StatusCode =
                            StatusCodes.Status500InternalServerError
                    };
                }

                // Connect to the ABC Retail CustomerProfiles table
                TableClient tableClient =
                    new TableClient(
                        connectionString,
                        "CustomerProfiles");

                await tableClient.CreateIfNotExistsAsync();

                // Create the Azure Table entity
                TableEntity entity =
                    new TableEntity(
                        "Customer",
                        Guid.NewGuid().ToString())
                    {
                        ["FirstName"] = customer.FirstName,
                        ["LastName"] = customer.LastName,
                        ["Email"] = customer.Email,
                        ["PhoneNumber"] = customer.PhoneNumber,
                        ["Address"] = customer.Address
                    };

                // Store customer in Azure Table Storage
                await tableClient.AddEntityAsync(entity);

                _logger.LogInformation(
                    "Customer {FirstName} {LastName} stored successfully.",
                    customer.FirstName,
                    customer.LastName);

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Customer stored successfully.",
                    rowKey = entity.RowKey
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid JSON received by StoreCustomer.");

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
                    "An error occurred while storing the customer.");

                return new ObjectResult(new
                {
                    success = false,
                    message = "Failed to store customer."
                })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}