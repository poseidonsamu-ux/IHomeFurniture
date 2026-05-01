using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class UserAccountController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // 1. GET: Hiển thị thông tin tài khoản
        [HttpGet]
        public ActionResult AccountInfor()
        {
            // Kiểm tra xem khách hàng đã đăng nhập chưa
            if (Session["TaiKhoan"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // LẤY DỮ LIỆU AN TOÀN: Coi Session["TaiKhoan"] là chuỗi chữ (username)
            string tenTaiKhoan = Session["TaiKhoan"].ToString();

            // Tìm kiếm khách hàng trong DB bằng tên tài khoản
            var kh = db.KHACHHANGs.SingleOrDefault(n => n.TaiKhoan == tenTaiKhoan);

            if (kh == null)
            {
                // Nếu không tìm thấy, xóa Session cũ và quay về trang Đăng nhập
                Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return View(kh);
        }

        // 2. POST: Cập nhật thông tin tài khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AccountInfor(FormCollection collection)
        {
            if (Session["TaiKhoan"] == null) return RedirectToAction("Login", "Account");

            string tenTaiKhoan = Session["TaiKhoan"].ToString();
            var kh = db.KHACHHANGs.SingleOrDefault(n => n.TaiKhoan == tenTaiKhoan);

            if (kh == null)
            {
                Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            try
            {
                string hoTen = collection["HoTen"];
                string email = collection["Email"];
                string dienThoai = collection["DienThoai"];
                string diaChi = collection["DiaChi"];

                // Kiểm tra dữ liệu hợp lệ
                if (string.IsNullOrEmpty(hoTen))
                {
                    ViewData["LoiHoTen"] = "Họ tên không được để trống";
                }
                if (string.IsNullOrEmpty(email))
                {
                    ViewData["LoiEmail"] = "Email không được để trống";
                }

                if (ModelState.IsValid && !string.IsNullOrEmpty(hoTen) && !string.IsNullOrEmpty(email))
                {
                    // Cập nhật dữ liệu mới
                    kh.HoTen = hoTen;
                    kh.Email = email;
                    kh.DienThoai = dienThoai;
                    kh.DiaChi = diaChi;

                    // Lưu thay đổi vào Database
                    db.SaveChanges();

                    // Cập nhật lại Session HoTen để hiển thị trên Layout ngay lập tức
                    Session["HoTen"] = kh.HoTen;

                    TempData["Success"] = "Cập nhật thông tin tài khoản thành công!";
                    return RedirectToAction("AccountInfor");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
            }

            return View(kh);
        }
    }
}