using System;
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

        // 2. Xem chi tiết bài viết và tăng lượt xem "thực tế"
        public ActionResult ChiTiet(int? id)
        {
            // 1. Kiểm tra ID tránh sập web
            if (id == null) return RedirectToAction("Index", "Home");

            // 2. Tìm bài viết theo ID
            var tin = db.TINTUCs.Find(id);
            if (tin == null) return HttpNotFound();

            // 3. LOGIC TĂNG LƯỢT XEM THỰC TẾ:
            // Tạo một mã khóa duy nhất cho bài viết này trong Session người dùng
            string sessionKey = "Viewed_Post_" + id;

            // Kiểm tra nếu người dùng chưa xem bài này trong phiên làm việc hiện tại
            if (Session[sessionKey] == null)
            {
                // Tăng lượt xem lên 1 đơn vị
                tin.LuotXem = (tin.LuotXem ?? 0) + 1;

                // Lưu lại thay đổi vào Database
                db.SaveChanges();

                // Đánh dấu Session là đã xem để nhấn F5 không tăng nữa
                Session[sessionKey] = true;
            }

            return View(tin);
        }
    }
}