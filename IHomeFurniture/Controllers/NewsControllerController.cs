using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class NewsController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // 1. Lấy danh sách tin tức mới nhất
        public ActionResult Index()
        {
            var listTin = db.TINTUCs.OrderByDescending(t => t.NgayDang).ToList();
            return View(listTin);
        }

        // 2. Xem chi tiết bài viết và tăng lượt xem
        public ActionResult ChiTiet(int? id)
        {
            // 1. Kiểm tra ID tránh sập web
            if (id == null) return RedirectToAction("Index", "Home");

            // 2. Tìm bài viết theo ID
            var tin = db.TINTUCs.Find(id);
            if (tin == null) return HttpNotFound();

            // 3. Tăng lượt xem bài viết
            tin.LuotXem = (tin.LuotXem ?? 0) + 1;
            db.SaveChanges();

            return View(tin);
        }
    }
}