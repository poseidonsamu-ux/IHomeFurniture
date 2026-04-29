using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class ProductController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        public ActionResult Index(int? categoryId, int? brandId, string priceRange, string searchTerm, string sortOrder, int page = 1)
        {
            int pageSize = 8;
            // Khởi tạo query duy nhất
            var query = db.SANPHAMs.Where(s => s.TrangThai == true).AsQueryable();

            // 1. Lấy dữ liệu cho bộ lọc Aside (Sidebar)
            ViewBag.Categories = db.DANHMUCs.ToList();
            ViewBag.Brands = db.THUONGHIEUx.ToList();

            // 2. Lọc theo từ khóa (Chỉ làm 1 lần duy nhất)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.TenSP.ToLower().Contains(searchTerm.ToLower()));
                ViewBag.SearchTerm = searchTerm;
            }

            // 3. Lọc danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(s => s.MaDM == categoryId.Value);
            }

            // 4. Lọc theo thương hiệu
            if (brandId.HasValue)
            {
                query = query.Where(s => s.MaTH == brandId.Value);
            }

            // 5. Lọc giá
            if (!string.IsNullOrEmpty(priceRange))
            {
                if (priceRange == "under2") query = query.Where(s => s.GiaBan < 2000000);
                else if (priceRange == "2to5") query = query.Where(s => s.GiaBan >= 2000000 && s.GiaBan <= 5000000);
                else if (priceRange == "5to10") query = query.Where(s => s.GiaBan > 5000000 && s.GiaBan <= 10000000);
                else if (priceRange == "over10") query = query.Where(s => s.GiaBan > 10000000);
            }

            // 6. Xử lý Sắp xếp (Tuyệt đối không gán lại OrderBy ở dưới nữa)
            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "price_asc":
                    query = query.OrderBy(s => s.GiaBan);
                    ViewBag.SortName = "Giá tăng dần";
                    break;
                case "price_desc":
                    query = query.OrderByDescending(s => s.GiaBan);
                    ViewBag.SortName = "Giá giảm dần";
                    break;
                case "best_seller":
                    query = query.OrderByDescending(s => s.LuotXem);
                    ViewBag.SortName = "Bán chạy";
                    break;
                default:
                    query = query.OrderByDescending(s => s.NgayCapNhat);
                    ViewBag.SortName = "Mới nhất";
                    break;
            }

            // 7. Tính toán phân trang
            int totalRow = query.Count();
            int totalPage = (int)Math.Ceiling((double)totalRow / pageSize);

            // Thực thi truy vấn lấy dữ liệu trang hiện tại
            var sanPhams = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // 8. Đồng bộ ViewBag qua View
            ViewBag.TotalPage = totalPage;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItem = totalRow;
            ViewBag.CategoryId = categoryId;
            ViewBag.BrandId = brandId;
            ViewBag.PriceRange = priceRange;
            ViewBag.SortOrder = sortOrder;

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