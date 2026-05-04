using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class BrandController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // 1. Danh sách các thương hiệu (Dành cho khách hàng xem)
        public ActionResult Index()
        {
            // Lấy danh sách thương hiệu, sắp xếp theo tên A-Z cho đẹp
            var list = db.THUONGHIEUx.OrderBy(t => t.TenTH).ToList();
            return View(list);
        }

        // 2. Chi tiết thương hiệu (Khi khách hàng click vào 1 hãng)
        public ActionResult ChiTiet(int? id)
        {
            // Nếu không có ID thì đẩy về trang danh sách hãng
            if (id == null) return RedirectToAction("Index");

            // Tìm thương hiệu theo ID
            var brand = db.THUONGHIEUx.Find(id);
            if (brand == null) return HttpNotFound();

            // (Tùy chọn) Lấy thêm danh sách sản phẩm thuộc hãng này để hiển thị kèm
            // Lọc các sản phẩm có MaTH bằng với id hãng và đang ở trạng thái mở bán
            ViewBag.ListSanPham = db.SANPHAMs.Where(s => s.MaTH == id && s.TrangThai == true).ToList();

            return View(brand);
        }
    }
}