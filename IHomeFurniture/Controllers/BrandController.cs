using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;

namespace IHomeFurniture.Controllers
{
    public class BrandController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        public ActionResult Index()
        {
            var list = db.THUONGHIEUx.OrderByDescending(t => t.MaTH).ToList();
            return View(list);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(THUONGHIEU brand)
        {
            if (ModelState.IsValid)
            {
                db.THUONGHIEUx.Add(brand);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(brand);
        }

        public ActionResult Delete(int id)
        {
            var brand = db.THUONGHIEUx.Find(id);
            if (brand != null)
            {
                db.THUONGHIEUx.Remove(brand);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}