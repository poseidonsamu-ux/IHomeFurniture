using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class NewsController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // 1. TRANG DANH SÁCH TẤT CẢ BÀI VIẾT
        public ActionResult Index()
        {
            // Lấy danh sách tin tức mới nhất đưa lên đầu
            var listTin = db.TINTUCs.OrderByDescending(t => t.NgayDang).ToList();
            return View(listTin);
        }

        // 2. TRANG ĐỌC CHI TIẾT 1 BÀI VIẾT (Đã fix chống sập web)
        public ActionResult ChiTiet(int? id)
        {
            // Chống cháy: Nếu người ta gõ thiếu ID, đá họ về lại trang chủ
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var tin = db.TINTUCs.Find(id);
            if (tin == null)
            {
                return HttpNotFound(); // Báo lỗi 404 nếu không tìm thấy bài
            }

            // Mỗi lần có người click vào đọc, tăng lượt xem lên 1
            tin.LuotXem = (tin.LuotXem ?? 0) + 1;
            db.SaveChanges();

            return View(tin);
        }
    }
}