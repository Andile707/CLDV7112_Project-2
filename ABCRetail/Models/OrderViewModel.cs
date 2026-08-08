using System.ComponentModel.DataAnnotations;

namespace ABCRetail.Models
{
    public class OrderViewModel
    {
        [Required]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int Quantity { get; set; }
    }
}
