using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace ABCRetail.Services
{
    public class ServiceBusService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServiceBusService> _logger;

        public ServiceBusService(
            IConfiguration configuration,
            ILogger<ServiceBusService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendMessageAsync(
            object messageData)
        {
            try
            {
                string? connectionString =
                    _configuration["AzureServiceBus:ConnectionString"];

                string? queueName =
                    _configuration["AzureServiceBus:QueueName"];

                if (string.IsNullOrWhiteSpace(connectionString) ||
                    string.IsNullOrWhiteSpace(queueName))
                {
                    _logger.LogWarning(
                        "Azure Service Bus configuration is missing.");

                    return;
                }

                await using ServiceBusClient client =
                    new ServiceBusClient(connectionString);

                ServiceBusSender sender =
                    client.CreateSender(queueName);

                string json =
                    JsonSerializer.Serialize(messageData);

                ServiceBusMessage message =
                    new ServiceBusMessage(json)
                    {
                        ContentType = "application/json"
                    };

                await sender.SendMessageAsync(message);

                _logger.LogInformation(
                    "Order notification sent to Azure Service Bus.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while sending a message " +
                    "to Azure Service Bus.");
            }
        }
    }
}
