using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models; // Nhớ sửa theo tên project của bạn

namespace IHomeFurniture.Controllers
{
    public class AdminController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // =====================================
        // 1. ĐĂNG NHẬP DÀNH CHO ADMIN
        // =====================================
        public ActionResult Login()
        {
            if (Session["Admin_ID"] != null) return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public ActionResult Login(string TenDN, string MatKhau)
        {
            var admin = db.ADMINs.FirstOrDefault(a => a.TenDN == TenDN && a.MatKhau == MatKhau && a.TrangThai == true);

            if (admin != null)
            {
                Session["Admin_ID"] = admin.MaAd;
                Session["Admin_Name"] = admin.HoTen;
                Session["Admin_Role"] = admin.Quyen;
                return RedirectToAction("Index");
            }

            ViewBag.Error = "Tài khoản hoặc mật khẩu quản trị không đúng!";
            return View();
        }

        public ActionResult Logout()
        {
            Session["Admin_ID"] = null;
            Session["Admin_Name"] = null;
            Session["Admin_Role"] = null;
            return RedirectToAction("Login");
        }

        // =====================================
        // 2. DASHBOARD (TRANG CHỦ ADMIN)
        // =====================================
        public ActionResult Index()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");

            ViewBag.TotalUsers = db.KHACHHANGs.Count();
            ViewBag.TotalProducts = db.SANPHAMs.Count();
            ViewBag.TotalOrders = db.DONDATHANGs.Count();
            ViewBag.TotalRevenue = db.DONDATHANGs.Where(d => d.MaTT == 4).Sum(d => (decimal?)d.TongTien) ?? 0;

            return View();
        }

        // =====================================
        // 3. QUẢN LÝ KHÁCH HÀNG
        // =====================================
        public ActionResult QuanLyKhachHang()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            var danhSachKH = db.KHACHHANGs.OrderByDescending(k => k.NgayTao).ToList();
            return View(danhSachKH);
        }

        // =====================================
        // 4. THÊM KHÁCH HÀNG (TỪ ADMIN)
        // =====================================
        public ActionResult ThemKhachHang()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public ActionResult ThemKhachHang(KHACHHANG kh)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            try
            {
                kh.NgayTao = DateTime.Now;
                kh.TrangThai = true;
                kh.DaXacThucEmail = true;
                db.KHACHHANGs.Add(kh);
                db.SaveChanges();
                return RedirectToAction("QuanLyKhachHang");
            }
            catch (Exception)
            {
                ViewBag.Error = "Tài khoản hoặc Email đã tồn tại, hoặc mật khẩu chưa đủ mạnh!";
                return View(kh);
            }
        }

        // =====================================
        // 5. SỬA THÔNG TIN KHÁCH HÀNG
        // =====================================
        public ActionResult SuaKhachHang(int id)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            var kh = db.KHACHHANGs.Find(id);
            if (kh == null) return HttpNotFound();
            return View(kh);
        }

        [HttpPost]
        public ActionResult SuaKhachHang(KHACHHANG model)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            try
            {
                var kh = db.KHACHHANGs.Find(model.MaKH);
                if (kh != null)
                {
                    kh.HoTen = model.HoTen;
                    kh.Email = model.Email;
                    kh.DienThoai = model.DienThoai;
                    kh.DiaChi = model.DiaChi;
                    kh.TrangThai = model.TrangThai;

                    db.SaveChanges();
                    return RedirectToAction("QuanLyKhachHang");
                }
            }
            catch { ViewBag.Error = "Lỗi cập nhật dữ liệu!"; }
            return View(model);
        }

        // =====================================
        // 6. XÓA (KHÓA) KHÁCH HÀNG
        // =====================================
        public ActionResult XoaKhachHang(int id)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            var kh = db.KHACHHANGs.Find(id);
            if (kh != null)
            {
                kh.TrangThai = false;
                db.SaveChanges();
            }
            return RedirectToAction("QuanLyKhachHang");
        }

        // =====================================
        // 7. XÓA VĨNH VIỄN (HARD DELETE)
        // =====================================
        public ActionResult XoaVinhVien(int id)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            try
            {
                var kh = db.KHACHHANGs.Find(id);
                if (kh != null)
                {
                    db.KHACHHANGs.Remove(kh);
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn vì khách hàng này đã có dữ liệu đơn hàng! Hãy sử dụng tính năng 'Khóa' thay thế.";
            }

            return RedirectToAction("QuanLyKhachHang");
        }
    }
}