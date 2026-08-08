namespace ABCRetail.Services
{
    public interface IQueueStorageService
    {
        Task SendMessageAsync(string message);
        Task<List<string>> GetMessagesAsync();
    }
}
