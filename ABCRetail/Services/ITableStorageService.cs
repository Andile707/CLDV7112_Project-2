using ABCRetail.Models;

namespace ABCRetail.Services
{
    public interface ITableStorageService
    {
        Task AddCustomerAsync(CustomerProfileEntity customer);

        Task<List<CustomerProfileEntity>> GetCustomersAsync();

        Task AddProductAsync(ProductEntity product);

        Task<List<ProductEntity>> GetProductsAsync();
    }
}
