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
        public ActionResult Index()
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
                return RedirectToAction("Index");
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
            return RedirectToAction("Index");
        }

        // 5. Chi tiết tin tức (Dùng cho người xem)
        public ActionResult ChiTiet(int? id)
        {
            // Kiểm tra nếu ID trống thì về trang chủ, tránh lỗi 500
            if (id == null) return RedirectToAction("Index", "Home");

            // Tìm bài viết theo mã ID
            var baiViet = db.TINTUCs.Find(id);

            // Nếu không tìm thấy bài viết
            if (baiViet == null) return HttpNotFound();

            // Tăng lượt xem (Check null để tránh lỗi cộng dồn)
            baiViet.LuotXem = (baiViet.LuotXem ?? 0) + 1;
            db.SaveChanges();

            return View(baiViet);
        }
    }
}