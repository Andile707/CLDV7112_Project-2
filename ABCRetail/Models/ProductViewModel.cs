using System.ComponentModel.DataAnnotations;

namespace ABCRetail.Models
{
    public class ProductViewModel
    {
        [Required]
        [Display(Name = "Product name")]
        public string ProductName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public double Price { get; set; }

        [Display(Name = "Quantity in stock")]
        [Range(0, int.MaxValue)]
        public int QuantityInStock { get; set; }

        [Required]
        [Display(Name = "Product image")]
        public IFormFile? ImageFile { get; set; }
    }
}