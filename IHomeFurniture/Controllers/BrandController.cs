using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class BrandController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // Trang danh sách thương hiệu cho khách xem
        public ActionResult Index()
        {
            // Lấy danh sách thương hiệu từ bảng THUONGHIEU trong SQL
            var list = db.THUONGHIEUx.OrderBy(t => t.TenTH).ToList();
            return View(list);
        }

        // Trang chi tiết 1 thương hiệu và các sản phẩm của nó
        public ActionResult ChiTiet(int? id)
        {
            if (id == null) return RedirectToAction("Index");

            var brand = db.THUONGHIEUx.Find(id);
            if (brand == null) return HttpNotFound();

            // Lấy thêm danh sách sản phẩm thuộc thương hiệu này
            ViewBag.ListSanPham = db.SANPHAMs.Where(s => s.MaTH == id && s.TrangThai == true).ToList();

            return View(brand);
        }
    }
}