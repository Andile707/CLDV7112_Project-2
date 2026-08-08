namespace ABCRetail.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(IFormFile imageFile);

        Task DeleteImageAsync(string imageUrl);
    }
}
