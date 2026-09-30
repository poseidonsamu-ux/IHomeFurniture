using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IHomeFurniture.Models; // Gọi namespace Models để dùng Database

namespace IHomeFurniture.Controllers
{
    // [Authorize(Roles = "Staff")] // Mở khóa dòng này sau khi bạn làm xong chức năng đăng nhập/phân quyền
    public class StaffController : Controller
    {
        // Khởi tạo kết nối CSDL (Thay IHomeEntities bằng tên class Context trong file .edmx của bạn)
        // private IHomeEntities db = new IHomeEntities();

        // 1. Dashboard - Xem thống kê nhanh
        // GET: Staff/Index hoac Staff/
        public ActionResult Index()
        {
            return View();
        }

        // 2. Quản lý đơn hàng - Xem và cập nhật trạng thái đơn
        // GET: Staff/QuanLyDonHang
        public ActionResult QuanLyDonHang()
        {
            // Code mẫu gọi DB: var donHangs = db.DONDATHANGs.ToList();
            // return View(donHangs);
            return View();
        }

        // 3. Quản lý khách hàng - Xem danh sách và lịch sử
        // GET: Staff/QuanLyKhachHang
        public ActionResult QuanLyKhachHang()
        {
            return View();
        }

        // 4. Theo dõi sản phẩm - Chỉ xem tồn kho
        // GET: Staff/TheoDoiSanPham
        public ActionResult TheoDoiSanPham()
        {
            return View();
        }

        // 5. Tư vấn khách hàng - Giao diện Chat
        // GET: Staff/TuVanKhachHang
        public ActionResult TuVanKhachHang()
        {
            return View();
        }

        // 6. Tài khoản cá nhân - Đổi mật khẩu/Cập nhật thông tin
        // GET: Staff/TaiKhoanCaNhan
        public ActionResult TaiKhoanCaNhan()
        {
            return View();
        }
    }
}