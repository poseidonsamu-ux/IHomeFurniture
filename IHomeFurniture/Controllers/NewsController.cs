using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class NewsController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // 1. Danh sách tin tức (Trang quản lý)
        public ActionResult QuanLyTinTuc()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");
            var list = db.TINTUCs.OrderByDescending(t => t.NgayDang).ToList();
            return View(list);
        }

        // 2. Thêm tin mới (Giao diện)
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");
            return View();
        }

        // 3. Thêm tin mới (Xử lý lưu)
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(TINTUC tin, HttpPostedFileBase fAnhTin)
        {
            try
            {
                if (fAnhTin != null && fAnhTin.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(fAnhTin.FileName);
                    string path = Path.Combine(Server.MapPath("~/Images/News/"), fileName);
                    fAnhTin.SaveAs(path);
                    tin.AnhTin = fileName;
                }
                tin.NgayDang = DateTime.Now;
                tin.LuotXem = 0;
                tin.MaAd = (int)Session["Admin_ID"];
                db.TINTUCs.Add(tin);
                db.SaveChanges();
                return RedirectToAction("QuanLyTinTuc"); // Đã sửa lại đường dẫn
            }
            catch { return View(tin); }
        }

        // 4. Xóa tin tức
        public ActionResult Delete(int id)
        {
            var tin = db.TINTUCs.Find(id);
            if (tin != null)
            {
                db.TINTUCs.Remove(tin);
                db.SaveChanges();
            }
            return RedirectToAction("QuanLyTinTuc"); // Đã sửa lại đường dẫn
        }

        // 5. Chi tiết tin tức (Dùng cho người xem)
        public ActionResult ChiTiet(int? id)
        {
            if (id == null) return RedirectToAction("DanhSach", "News");

            var baiViet = db.TINTUCs.Find(id);

            if (baiViet == null) return HttpNotFound();

            baiViet.LuotXem = (baiViet.LuotXem ?? 0) + 1;
            db.SaveChanges();

            return View(baiViet);
        }

        // 6. Sửa tin tức (Lấy dữ liệu cũ lên giao diện)
        [HttpGet]
        public ActionResult SuaTinTuc(int? id)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");
            if (id == null) return RedirectToAction("QuanLyTinTuc"); // Đã sửa lại đường dẫn

            var tin = db.TINTUCs.Find(id);
            if (tin == null) return HttpNotFound();

            return View(tin);
        }

        // 7. Sửa tin tức (Xử lý lưu vào Database)
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SuaTinTuc(TINTUC tin, HttpPostedFileBase fAnhTin)
        {
            try
            {
                var tinUpdate = db.TINTUCs.Find(tin.MaTin);
                if (tinUpdate != null)
                {
                    tinUpdate.TieuDe = tin.TieuDe;
                    tinUpdate.TomTat = tin.TomTat;
                    tinUpdate.NoiDung = tin.NoiDung;

                    if (fAnhTin != null && fAnhTin.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(fAnhTin.FileName);
                        string path = Path.Combine(Server.MapPath("~/Images/News/"), fileName);
                        fAnhTin.SaveAs(path);
                        tinUpdate.AnhTin = fileName;
                    }

                    db.SaveChanges();
                    return RedirectToAction("QuanLyTinTuc");
                }
                return View(tin);
            }
            catch
            {
                return View(tin);
            }
        }

        // 8. Danh sách tin tức (Dùng cho người xem)
        public ActionResult DanhSach()
        {
            var list = db.TINTUCs.OrderByDescending(t => t.NgayDang).ToList();
            return View(list);
        }
    }
}