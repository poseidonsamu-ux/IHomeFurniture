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

        public ActionResult Index()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");
            var list = db.TINTUCs.OrderByDescending(t => t.NgayDang).ToList();
            return View(list);
        }

        // 1. Thêm tin mới
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");
            return View();
        }

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

        // 2. Xóa tin tức
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
    }
}