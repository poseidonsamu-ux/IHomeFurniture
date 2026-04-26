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
            // 1. Lấy 8 sản phẩm mới nhất (sắp xếp giảm dần theo mã SP) làm Model chính
            var sanPhamMoi = db.SANPHAMs.OrderByDescending(s => s.MaSP).Take(8).ToList();

            // 2. Lấy 3 tin tức mới nhất (Truyền qua ViewBag để hiển thị ở Góc Cảm Hứng)
            ViewBag.TinTucMoi = db.TINTUCs.OrderByDescending(t => t.NgayDang).Take(3).ToList();

            // 3. Đẩy danh sách sản phẩm ra ngoài giao diện
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