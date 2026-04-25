using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class CartController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // 1. Hàm lấy giỏ hàng từ Session
        public List<CartItem> GetCart()
        {
            List<CartItem> cart = Session["Cart"] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                Session["Cart"] = cart;
            }
            return cart;
        }

        // 2. Trang hiển thị Giỏ hàng
        public ActionResult Index()
        {
            var cart = GetCart();
            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
            return View(cart);
        }

        // 3. Hàm Thêm vào giỏ hàng (Dùng AJAX trả về JSON)
        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            var cart = GetCart();
            var product = db.SANPHAMs.FirstOrDefault(p => p.MaSP == productId);

            if (product != null)
            {
                var existingItem = cart.FirstOrDefault(i => i.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity; // Nếu có rồi thì tăng số lượng
                }
                else
                {
                    // Lấy giá khuyến mãi nếu có, không thì lấy giá bán
                    double currentPrice = (product.GiaKhuyenMai > 0 && product.GiaKhuyenMai < product.GiaBan)
                                          ? (double)product.GiaKhuyenMai
                                          : (double)product.GiaBan;

                    cart.Add(new CartItem
                    {
                        ProductId = product.MaSP,
                        ProductName = product.TenSP,
                        Image = product.AnhBia,
                        Price = currentPrice,
                        Quantity = quantity
                    });
                }
                Session["Cart"] = cart;
            }

            // Trả về tổng số lượng sản phẩm đang có trong giỏ để update lên Header
            int totalItems = cart.Sum(i => i.Quantity);
            return Json(new { success = true, cartCount = totalItems });
        }

        // 4. Hàm xóa 1 món khỏi giỏ
        public ActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var itemToRemove = cart.FirstOrDefault(i => i.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                Session["Cart"] = cart;
            }
            return RedirectToAction("Index");
        }
    }
}