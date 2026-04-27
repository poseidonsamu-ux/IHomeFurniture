using System;

namespace IHomeFurniture.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        // Tự động tính tổng tiền của món này (Giá x Số lượng)
        public double TotalPrice
        {
            get { return Price * Quantity; }
        }
    }
}