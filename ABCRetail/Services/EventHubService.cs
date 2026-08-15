using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
using System.Text.Json;

namespace ABCRetail.Services
{
    public class EventHubService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventHubService> _logger;

        public EventHubService(
            IConfiguration configuration,
            ILogger<EventHubService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEventAsync(
            string eventType,
            object eventData)
        {
            try
            {
                string? connectionString =
                    _configuration["AzureEventHubs:ConnectionString"];

                string? eventHubName =
                    _configuration["AzureEventHubs:EventHubName"];

                if (string.IsNullOrWhiteSpace(connectionString) ||
                    string.IsNullOrWhiteSpace(eventHubName))
                {
                    _logger.LogWarning(
                        "Azure Event Hubs configuration is missing.");

                    return;
                }

                await using EventHubProducerClient producer =
                    new EventHubProducerClient(
                        connectionString,
                        eventHubName);

                var eventObject = new
                {
                    EventType = eventType,
                    Timestamp = DateTime.UtcNow,
                    Data = eventData
                };

                string json =
                    JsonSerializer.Serialize(eventObject);

                using EventDataBatch eventBatch =
                    await producer.CreateBatchAsync();

                EventData eventMessage =
                    new EventData(
                        Encoding.UTF8.GetBytes(json));

                if (!eventBatch.TryAdd(eventMessage))
                {
                    _logger.LogError(
                        "Event {EventType} was too large for the Event Hub batch.",
                        eventType);

                    return;
                }

                await producer.SendAsync(eventBatch);

                _logger.LogInformation(
                    "Event {EventType} successfully sent to Event Hubs.",
                    eventType);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while sending event {EventType} to Azure Event Hubs.",
                    eventType);
            }
        }
    }
}