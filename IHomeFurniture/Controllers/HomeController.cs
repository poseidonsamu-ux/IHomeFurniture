using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class HomeController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        public ActionResult Index()
        {
            // 2. Lấy 8 sản phẩm mới nhất (sắp xếp giảm dần theo ngày cập nhật hoặc mã SP)
            // Sếp có thể đổi s.MaSP thành s.NgayCapNhat nếu trong Database có cột đó nha
            var sanPhamMoi = db.SANPHAMs.OrderByDescending(s => s.MaSP).Take(8).ToList();

            // 3. Đẩy danh sách 8 sản phẩm này ra ngoài giao diện Index.cshtml
            return View(sanPhamMoi);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
    }
}