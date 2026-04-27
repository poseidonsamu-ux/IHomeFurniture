using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class CartController : Controller
    {
        // KẾT NỐI TỚI DATABASE
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
        public ActionResult Add(int productId, int quantity = 1)
        {
            var cart = GetCart();
            var product = db.SANPHAMs.FirstOrDefault(p => p.MaSP == productId);

            if (product != null)
            {
                var existingItem = cart.FirstOrDefault(i => i.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
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

        // ==========================================
        // PHẦN XỬ LÝ THANH TOÁN (Bám sát CSDL 100%)
        // ==========================================

        // 5. Hiển thị trang Thanh Toán (GET)
        [HttpGet]
        public ActionResult Checkout()
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }
            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
            return View(cart);
        }

        // 6. Xử lý khi khách bấm nút Xác nhận đặt hàng (POST)
        [HttpPost]
        public ActionResult Checkout(string hoTen, string soDienThoai, string diaChi, string ghiChu)
        {
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            try
            {
                // -- A. LƯU VÀO BẢNG DONDATHANG --
                DONDATHANG donHangMoi = new DONDATHANG();
                donHangMoi.NgayDat = DateTime.Now;
                donHangMoi.DienThoaiNhan = soDienThoai;
                donHangMoi.DiaChiGiaoHang = diaChi;
                donHangMoi.TongTien = (decimal)cart.Sum(item => item.TotalPrice);
                donHangMoi.MaTT = 1; // 1 = Chờ xác nhận (Dựa theo SQL của sếp)
                donHangMoi.MaPT = 1; // 1 = COD (Mặc định tạm, sếp nâng cấp phương thức thanh toán sau)

                // Ghép Tên Người Nhận vào Ghi Chú (Vì SQL không có cột Tên Người Nhận)
                donHangMoi.GhiChu = "Người nhận: " + hoTen + ". Ghi chú: " + ghiChu;

                // Nếu khách đã đăng nhập (Có Session MaKH) thì gán vào đơn hàng
                if (Session["MaKH"] != null)
                {
                    donHangMoi.MaKH = (int)Session["MaKH"];
                }

                db.DONDATHANGs.Add(donHangMoi);
                db.SaveChanges(); // SQL sẽ tự sinh ra MaDonHang

                // -- B. LƯU VÀO BẢNG CHITIETDATHANG --
                foreach (var item in cart)
                {
                    CHITIETDATHANG chiTiet = new CHITIETDATHANG();
                    chiTiet.MaDonHang = donHangMoi.MaDonHang;   // Lấy Mã sinh ra ở trên
                    chiTiet.MaSP = item.ProductId;
                    chiTiet.SoLuong = item.Quantity;
                    chiTiet.DonGia = (decimal)item.Price;

                    db.CHITIETDATHANGs.Add(chiTiet);
                }
                db.SaveChanges();

                // -- C. DỌN SẠCH GIỎ HÀNG --
                Session["Cart"] = null;

                // Chuyển sang trang báo thành công
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
                ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
                return View(cart);
            }
        }

        // 7. Trang báo đặt hàng thành công
        public ActionResult Success()
        {
            return View();
        }
    }
}