using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;
using System.Net;
using System.Net.Mail;

namespace IHomeFurniture.Controllers
{
    public class AccountController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // ==========================================
        // 1. ĐĂNG KÝ
        // ==========================================
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                var check = db.KHACHHANGs.FirstOrDefault(s => s.TaiKhoan == model.TaiKhoan || s.Email == model.Email);
                if (check != null)
                {
                    ViewBag.Error = "Tài khoản hoặc Email đã tồn tại!";
                    return View(model);
                }

                Random rd = new Random();
                string otp = rd.Next(100000, 999999).ToString();

                var kh = new KHACHHANG
                {
                    HoTen = model.HoTen,
                    TaiKhoan = model.TaiKhoan,
                    MatKhau = model.MatKhau,
                    Email = model.Email,
                    MaXacNhan = otp,
                    ThoiGianHetHanOTP = DateTime.Now.AddMinutes(3),
                    DaXacThucEmail = false,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                db.KHACHHANGs.Add(kh);
                db.SaveChanges();

                if (SendEmail(model.Email, otp))
                {
                    return RedirectToAction("VerifyOtp", new { email = model.Email });
                }
                else
                {
                    ViewBag.Error = "Lỗi hệ thống khi gửi Email. Vui lòng thử lại!";
                }
            }
            return View(model);
        }

        // ==========================================
        // 2. XÁC THỰC OTP
        // ==========================================
        public ActionResult VerifyOtp(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public ActionResult VerifyOtp(string email, string otp)
        {
            var kh = db.KHACHHANGs.FirstOrDefault(x => x.Email == email && x.MaXacNhan == otp);

            if (kh != null)
            {
                if (kh.ThoiGianHetHanOTP < DateTime.Now)
                {
                    ViewBag.Error = "Mã OTP đã hết hạn!";
                    ViewBag.Email = email;
                    return View();
                }

                kh.DaXacThucEmail = true;
                kh.MaXacNhan = null;
                db.SaveChanges();

                return RedirectToAction("Login");
            }

            ViewBag.Error = "Mã OTP không chính xác!";
            ViewBag.Email = email;
            return View();
        }

        // ==========================================
        // 3. GỬI LẠI MÃ OTP
        // ==========================================
        public ActionResult ResendOtp(string email)
        {
            var kh = db.KHACHHANGs.FirstOrDefault(x => x.Email == email);
            if (kh != null)
            {
                string newOtp = new Random().Next(100000, 999999).ToString();
                kh.MaXacNhan = newOtp;
                kh.ThoiGianHetHanOTP = DateTime.Now.AddMinutes(3);
                db.SaveChanges();
                SendEmail(email, newOtp);
            }
            return RedirectToAction("VerifyOtp", new { email = email });
        }

        // ==========================================
        // 4. ĐĂNG NHẬP
        // ==========================================
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string TaiKhoan, string MatKhau)
        {
            var user = db.KHACHHANGs.FirstOrDefault(u => u.TaiKhoan == TaiKhoan && u.MatKhau == MatKhau);

            if (user != null)
            {
                if (user.DaXacThucEmail == false)
                {
                    ViewBag.Error = "Tài khoản chưa được xác thực Email!";
                    return View();
                }

                Session["MaKH"] = user.MaKH;
                Session["TaiKhoan"] = user.TaiKhoan;
                Session["HoTen"] = user.HoTen;

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
            return View();
        }

        // ==========================================
        // 5. ĐĂNG XUẤT
        // ==========================================
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 6. HÀM GỬI MAIL (ĐÃ NẰM TRONG CLASS)
        // ==========================================
        private bool SendEmail(string toEmail, string otp)
        {
            try
            {
                var fromAddress = new MailAddress("bnzune22@gmail.com", "iHOME Interior");
                var toAddress = new MailAddress(toEmail);
                string fromPassword = "kvkbhmeffznfmcsk";

                string subject = "Ma OTP xac thuc tai khoan iHOME";
                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 10px; overflow: hidden;'>
                        <div style='background: #000; color: #fff; padding: 20px; text-align: center;'>
                            <h1 style='margin: 0;'>iHOME.</h1>
                        </div>
                        <div style='padding: 30px; line-height: 1.6;'>
                            <p>Chào bạn,</p>
                            <p>Mã OTP để hoàn tất quá trình đăng ký tài khoản của bạn là:</p>
                            <div style='background: #f9f9f9; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 10px; color: #000; border: 1px dashed #ccc; margin: 20px 0;'>
                                {otp}
                            </div>
                            <p style='color: #666;'>Mã này sẽ hết hạn sau <strong>3 phút</strong>.</p>
                        </div>
                        <div style='background: #f4f4f4; padding: 15px; text-align: center; font-size: 12px; color: #999;'>
                            © 2026 iHome Interior. All rights reserved.
                        </div>
                    </div>";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body, IsBodyHtml = true })
                {
                    smtp.Send(message);
                }
                return true;
            }
            catch { return false; }
        }
    }
}