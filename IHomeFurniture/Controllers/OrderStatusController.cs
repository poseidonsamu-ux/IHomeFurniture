using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class OrderStatusController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // 1. LIST: Danh sách trạng thái
        public ActionResult Index()
        {
            // Kiểm tra đăng nhập Admin
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");

            var statuses = db.TRANGTHAIDONHANGs.ToList();
            return View(statuses);
        }

        // 2. CREATE (GET): Hiển thị form thêm mới
        public ActionResult Create()
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");
            return View();
        }

        // 2. CREATE (POST): Xử lý lưu trạng thái mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TRANGTHAIDONHANG status)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");

            if (ModelState.IsValid)
            {
                db.TRANGTHAIDONHANGs.Add(status);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(status);
        }

        // 3. EDIT (GET): Hiển thị form sửa trạng thái
        public ActionResult Edit(int? id)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");
            if (id == null) return RedirectToAction("Index");

            var status = db.TRANGTHAIDONHANGs.Find(id);
            if (status == null) return HttpNotFound();

            return View(status);
        }

        // 3. EDIT (POST): Xử lý lưu cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(TRANGTHAIDONHANG model)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");

            if (ModelState.IsValid)
            {
                var status = db.TRANGTHAIDONHANGs.Find(model.MaTT);
                if (status != null)
                {
                    status.TenTrangThai = model.TenTrangThai;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(model);
        }

        // 4. DELETE: Xóa trạng thái
        public ActionResult Delete(int id)
        {
            if (Session["Admin_ID"] == null) return RedirectToAction("Login", "Admin");

            try
            {
                var status = db.TRANGTHAIDONHANGs.Find(id);
                if (status != null)
                {
                    db.TRANGTHAIDONHANGs.Remove(status);
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                // Bắt lỗi nếu trạng thái này đang được dùng bởi một đơn hàng nào đó (Ràng buộc khóa ngoại)
                TempData["Error"] = "Không thể xóa trạng thái này vì đang có đơn hàng sử dụng nó!";
            }

            return RedirectToAction("Index");
        }
    }
}