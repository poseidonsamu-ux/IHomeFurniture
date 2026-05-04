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



        [HttpGet]
        public ActionResult Checkout()
        {
            // Kiểm tra giỏ hàng
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            // Bắt buộc đăng nhập: Nếu chưa đăng nhập thì chuyển hướng tới trang Đăng nhập
            if (Session["TaiKhoan"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy tên tài khoản từ Session dưới dạng string
            string tenTaiKhoan = Session["TaiKhoan"].ToString();

            // Tìm đối tượng KHACHHANG trong Database theo đúng thuộc tính TaiKhoan của Model
            var kh = db.KHACHHANGs.SingleOrDefault(n => n.TaiKhoan == tenTaiKhoan);

            if (kh != null)
            {
                // Đổ dữ liệu khách hàng (HoTen, DienThoai, DiaChi) vào ViewBag để View dùng
                ViewBag.KhachHang = kh;
            }

            // Lấy danh sách phương thức thanh toán
            ViewBag.PhuongThucTT = db.PHUONGTHUCTHANHTOANs.ToList();
            ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);

            return View(cart);
        }

        // 2. POST: Xử lý lưu đơn hàng khi khách bấm nút Xác nhận
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string hoTen, string soDienThoai, string diaChi, string ghiChu, int maPT)
        {
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            if (Session["TaiKhoan"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy lại thông tin khách hàng từ database
            string tenTaiKhoan = Session["TaiKhoan"].ToString();
            var kh = db.KHACHHANGs.SingleOrDefault(n => n.TaiKhoan == tenTaiKhoan);

            try
            {
                if (string.IsNullOrEmpty(hoTen) || string.IsNullOrEmpty(soDienThoai) || string.IsNullOrEmpty(diaChi))
                {
                    ViewBag.Error = "Vui lòng nhập đầy đủ họ tên, số điện thoại và địa chỉ giao hàng!";
                    ViewBag.PhuongThucTT = db.PHUONGTHUCTHANHTOANs.ToList();
                    ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
                    ViewBag.KhachHang = kh;
                    return View(cart);
                }

                // Cập nhật lại thông tin mới nhất vào Database nếu khách hàng có sửa đổi trên Form
                if (kh != null)
                {
                    kh.HoTen = hoTen;
                    kh.DienThoai = soDienThoai;
                    kh.DiaChi = diaChi;

                    db.Entry(kh).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges(); // Lưu ngay thông tin khách hàng
                }

                // -- A. LƯU VÀO BẢNG DONDATHANG --
                DONDATHANG donHangMoi = new DONDATHANG();
                donHangMoi.NgayDat = DateTime.Now;
                donHangMoi.DienThoaiNhan = soDienThoai;
                donHangMoi.DiaChiGiaoHang = diaChi;
                donHangMoi.TongTien = (decimal)cart.Sum(item => item.TotalPrice);
                donHangMoi.MaTT = 1; // 1 = Chờ xác nhận
                donHangMoi.MaPT = maPT;
                donHangMoi.MaKH = kh != null ? kh.MaKH : (int?)null;

                // Ghép Tên Người Nhận vào Ghi Chú
                donHangMoi.GhiChu = "Người nhận: " + hoTen + ". " + (string.IsNullOrEmpty(ghiChu) ? "" : "Ghi chú: " + ghiChu);

                db.DONDATHANGs.Add(donHangMoi);
                db.SaveChanges(); // SQL sinh ra MaDonHang

                // -- B. LƯU VÀO BẢNG CHITIETDATHANG --
                foreach (var item in cart)
                {
                    CHITIETDATHANG chiTiet = new CHITIETDATHANG();
                    chiTiet.MaDonHang = donHangMoi.MaDonHang;
                    chiTiet.MaSP = item.ProductId;
                    chiTiet.SoLuong = item.Quantity;
                    chiTiet.DonGia = (decimal)item.Price;

                    db.CHITIETDATHANGs.Add(chiTiet);
                }
                db.SaveChanges();

                // -- C. DỌN SẠCH GIỎ HÀNG --
                Session["Cart"] = null;

                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
                ViewBag.PhuongThucTT = db.PHUONGTHUCTHANHTOANs.ToList();
                ViewBag.TotalAmount = cart.Sum(item => item.TotalPrice);
                ViewBag.KhachHang = kh;
                return View(cart);
            }
        }

        public ActionResult Success()
        {
            return View();
        }
    }
}