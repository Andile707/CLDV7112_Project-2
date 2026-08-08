using ABCRetail.Models;
using Azure.Data.Tables;

namespace ABCRetail.Services
{
    public class TableStorageService : ITableStorageService
    {
        private readonly TableClient _customerTableClient;
        private readonly TableClient _productTableClient;

        public TableStorageService(IConfiguration configuration)
        {
            string connectionString =
                configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "The Azure Storage connection string is missing.");

            _customerTableClient = new TableClient(
                connectionString,
                "CustomerProfiles");

            _productTableClient = new TableClient(
                connectionString,
                "Products");
        }

        private async Task CreateTablesAsync()
        {
            await _customerTableClient.CreateIfNotExistsAsync();
            await _productTableClient.CreateIfNotExistsAsync();
        }

        public async Task AddCustomerAsync(
            CustomerProfileEntity customer)
        {
            ArgumentNullException.ThrowIfNull(customer);

            await CreateTablesAsync();

            customer.PartitionKey = "Customer";

            if (string.IsNullOrWhiteSpace(customer.RowKey))
            {
                customer.RowKey = Guid.NewGuid().ToString();
            }

            await _customerTableClient.AddEntityAsync(customer);
        }

        public async Task<List<CustomerProfileEntity>>
            GetCustomersAsync()
        {
            await CreateTablesAsync();

            var customers = new List<CustomerProfileEntity>();

            await foreach (
                CustomerProfileEntity customer in
                _customerTableClient.QueryAsync<CustomerProfileEntity>())
            {
                customers.Add(customer);
            }

            return customers
                .OrderBy(customer => customer.FirstName)
                .ThenBy(customer => customer.LastName)
                .ToList();
        }

        public async Task AddProductAsync(ProductEntity product)
        {
            ArgumentNullException.ThrowIfNull(product);

            await CreateTablesAsync();

            product.PartitionKey = "Product";

            if (string.IsNullOrWhiteSpace(product.RowKey))
            {
                product.RowKey = Guid.NewGuid().ToString();
            }

            await _productTableClient.AddEntityAsync(product);
        }

        public async Task<List<ProductEntity>> GetProductsAsync()
        {
            await CreateTablesAsync();

            var products = new List<ProductEntity>();

            await foreach (
                ProductEntity product in
                _productTableClient.QueryAsync<ProductEntity>())
            {
                products.Add(product);
            }

            return products
                .OrderBy(product => product.ProductName)
                .ToList();
        }
    }
}
