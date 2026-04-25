using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class ProductController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        public ActionResult Index(int? categoryId, string priceRange, string searchTerm, int page = 1)
        {
            int pageSize = 8;
            var query = db.SANPHAMs.Where(s => s.TrangThai == true).AsQueryable();

            // 1. Lọc theo từ khóa (Từ ô Tìm kiếm hoặc Menu Bộ sưu tập truyền xuống)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.TenSP.ToLower().Contains(searchTerm.ToLower()));
            }

            // 2. Lọc danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(s => s.MaDM == categoryId.Value);
            }

            // 3. Lọc giá
            if (!string.IsNullOrEmpty(priceRange))
            {
                if (priceRange == "under2") query = query.Where(s => s.GiaBan < 2000000);
                else if (priceRange == "2to5") query = query.Where(s => s.GiaBan >= 2000000 && s.GiaBan <= 5000000);
                else if (priceRange == "5to10") query = query.Where(s => s.GiaBan > 5000000 && s.GiaBan <= 10000000);
                else if (priceRange == "over10") query = query.Where(s => s.GiaBan > 10000000);
            }

            // Sắp xếp mới nhất
            query = query.OrderByDescending(s => s.NgayCapNhat);

            // Phân trang
            int totalRow = query.Count();
            int totalPage = (int)Math.Ceiling((double)totalRow / pageSize);
            var sanPhams = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Gửi dữ liệu qua View
            ViewBag.TotalPage = totalPage;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItem = totalRow;
            ViewBag.CategoryId = categoryId;
            ViewBag.PriceRange = priceRange;
            ViewBag.SearchTerm = searchTerm; // Kẹp thêm từ khóa để giữ bộ lọc

            return View(sanPhams);
        }

        public ActionResult Detail(int id)
        {
            var sp = db.SANPHAMs.FirstOrDefault(s => s.MaSP == id && s.TrangThai == true);
            if (sp == null) return RedirectToAction("Index", "Home");

            sp.LuotXem = (sp.LuotXem ?? 0) + 1;
            db.SaveChanges();

            ViewBag.SanPhamCungLoai = db.SANPHAMs
                .Where(s => s.MaDM == sp.MaDM && s.MaSP != sp.MaSP && s.TrangThai == true)
                .Take(4).ToList();

            return View(sp);
        }
    }
}