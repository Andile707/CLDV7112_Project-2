using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ABCRetail.Models
{
    public class ProductEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";

        public string RowKey { get; set; } =
            Guid.NewGuid().ToString();

        [Required]
        [Display(Name = "Product name")]
        public string ProductName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public double Price { get; set; }

        [Display(Name = "Quantity in stock")]
        [Range(0, int.MaxValue)]
        public int QuantityInStock { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }
    }
}