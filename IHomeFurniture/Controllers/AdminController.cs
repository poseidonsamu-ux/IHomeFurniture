using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class AdminController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // 1. Đăng nhập & đăng xuất admin
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
            Session.Clear();
            return RedirectToAction("Login");
        }

        // 2. Dashboard (trang chủ tổng quan)
        public ActionResult Index()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");

            ViewBag.TotalUsers = db.KHACHHANGs.Count();
            ViewBag.TotalProducts = db.SANPHAMs.Count();
            ViewBag.TotalOrders = db.DONDATHANGs.Count();
            ViewBag.TotalRevenue = db.DONDATHANGs.Where(d => d.MaTT == 4).Sum(d => (decimal?)d.TongTien) ?? 0;

            return View();
        }

        // 3. Quản lý khách hàng
        public ActionResult QuanLyKhachHang()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            var danhSachKH = db.KHACHHANGs.OrderByDescending(k => k.NgayTao).ToList();
            return View(danhSachKH);
        }

        // 4. Thêm khách hàng mới
        public ActionResult ThemKhachHang()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                ViewBag.Error = "Tài khoản/Email đã tồn tại, hoặc dữ liệu không hợp lệ!";
                return View(kh);
            }
        }

        // 5. Sửa thông tin khách hàng
        public ActionResult SuaKhachHang(int id)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");
            var kh = db.KHACHHANGs.Find(id);
            if (kh == null) return HttpNotFound();
            return View(kh);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
            catch { ViewBag.Error = "Lỗi cập nhật dữ liệu. Vui lòng kiểm tra lại!"; }
            return View(model);
        }

        // 6. Xóa mềm (khóa) khách hàng
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

        // 7. Xóa vĩnh viễn khách hàng
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
                TempData["Error"] = "Không thể xóa vĩnh viễn vì khách hàng này đã phát sinh đơn hàng!";
            }

            return RedirectToAction("QuanLyKhachHang");
        }

        // 8. Quản lý sản phẩm (nội thất)
        public ActionResult QuanLySanPham()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");

            // Lấy danh sách sản phẩm mới nhất
            var danhSachSP = db.SANPHAMs.OrderByDescending(s => s.MaSP).ToList();
            return View(danhSachSP);
        }

        // 9. Thêm sản phẩm mới (nội thất)
        public ActionResult ThemSanPham()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");

            // Load đúng bảng DANHMUC và THUONGHIEU
            ViewBag.MaDM = new SelectList(db.DANHMUCs.ToList(), "MaDM", "TenDanhMuc");
            ViewBag.MaTH = new SelectList(db.THUONGHIEUx.ToList(), "MaTH", "TenTH");

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ThemSanPham(SANPHAM sp, HttpPostedFileBase HinhAnh)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");

            try
            {
                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(HinhAnh.FileName);
                    string path = Path.Combine(Server.MapPath("~/Images/"), fileName);

                    HinhAnh.SaveAs(path);
                    sp.AnhBia = fileName;
                }

                // Gán các giá trị mặc định tránh lỗi NULL
                sp.NgayCapNhat = DateTime.Now;
                sp.TrangThai = true;
                sp.LuotXem = 0;

                db.SANPHAMs.Add(sp);
                db.SaveChanges();

                return RedirectToAction("QuanLySanPham");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi thêm sản phẩm: " + ex.Message;

                // Nếu lỗi thì load lại Dropdown list đúng bảng
                ViewBag.MaDM = new SelectList(db.DANHMUCs.ToList(), "MaDM", "TenDanhMuc", sp.MaDM);
                ViewBag.MaTH = new SelectList(db.THUONGHIEUx.ToList(), "MaTH", "TenTH", sp.MaTH);

                return View(sp);
            }
        }

        // 10. Quản lý đơn hàng
        public ActionResult QuanLyDonHang()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");

            var danhSachDH = db.DONDATHANGs.OrderByDescending(d => d.NgayDat).ToList();
            return View(danhSachDH);
        }

        
        // 11. Xuất báo cáo danh sách đơn hàng (tất cả trạng thái)
        public ActionResult XuatBaoCaoDoanhThu()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login");

            // BƯỚC QUAN TRỌNG: Xóa .Where để lấy toàn bộ 100% đơn hàng trong database
            var danhSachDonHang = db.DONDATHANGs.OrderByDescending(d => d.NgayDat).ToList();

            StringWriter sw = new StringWriter();

            // Thêm cột "Trang Thai" vào đầu trang Excel
            sw.WriteLine("Ma Don Hang,Ngay Dat,Khach Hang,So Dien Thoai,Tong Tien (VND),Trang Thai");

            foreach (var don in danhSachDonHang)
            {
                string tenKhachHang = don.KHACHHANG != null ? don.KHACHHANG.HoTen : "Khach Le";
                string ngayDat = don.NgayDat.HasValue ? don.NgayDat.Value.ToString("dd/MM/yyyy HH:mm") : "";
                string tongTien = don.TongTien.HasValue ? don.TongTien.Value.ToString("0") : "0";

                // Lấy tên trạng thái (Đã giao, Đã hủy, Chờ xác nhận...)
                string trangThai = don.TRANGTHAIDONHANG != null ? don.TRANGTHAIDONHANG.TenTrangThai : "Khong xac dinh";

                // Ghi dữ liệu bao gồm cả trạng thái đơn hàng
                sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5}",
                    don.MaDonHang,
                    ngayDat,
                    tenKhachHang,
                    don.DienThoaiNhan,
                    tongTien,
                    trangThai));
            }

            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment;filename=DanhSachDonHang_TongHop.csv");
            Response.ContentType = "text/csv";
            Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
            Response.Write(sw.ToString());
            Response.End();

            return new EmptyResult();
        }
    }
}