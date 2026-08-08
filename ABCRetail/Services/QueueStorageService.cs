using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ABCRetail.Services
{
    public class QueueStorageService : IQueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration configuration)
        {
            string connectionString =
                configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");

            _queueClient = new QueueClient(
                connectionString,
                "order-inventory-queue");
        }

        public async Task SendMessageAsync(string message)
        {
            await _queueClient.CreateIfNotExistsAsync();

            await _queueClient.SendMessageAsync(message);
        }

        public async Task<List<string>> GetMessagesAsync()
        {
            await _queueClient.CreateIfNotExistsAsync();

            PeekedMessage[] messages =
                await _queueClient.PeekMessagesAsync(
                    maxMessages: 20);

            return messages
                .Select(m => m.MessageText)
                .ToList();
        }
    }
}
