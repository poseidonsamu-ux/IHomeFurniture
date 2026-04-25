using System;
using System.Linq;

namespace IHomeFurniture.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public double TotalPrice => Price * Quantity; // Tính tổng tiền của món đó
    }
}