using System;
using System.Collections.Generic;
using System.Text;

namespace ABCRetail.functions.Models
{
    public class OrderFunctionModel
    {
        public string OrderNumber { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }
}
