using System;
using System.Linq;
using System.Web.Mvc;
using IHomeFurniture.Models;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace IHomeFurniture.Controllers
{
    public class AccountController : Controller
    {
        IHomeFurnitureEntities db = new IHomeFurnitureEntities();

        // 1. đăng ký
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

                if (model.MatKhau.Length < 8)
                {
                    ViewBag.Error = "Mật khẩu phải có độ dài từ 8 ký tự trở lên!";
                    return View(model);
                }

                if (!Regex.IsMatch(model.MatKhau, @"[^a-zA-Z0-9]"))
                {
                    ViewBag.Error = "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (VD: @, #, $, !,...)";
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

        // 2. xác thực otp
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

        // 3. gửi lại mã otp
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

        // 4. đăng nhập
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

        // 5. đăng xuất
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // 6. hàm gửi mail 
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
                            <p>Mã OTP của bạn là:</p>
                            <div style='background: #f9f9f9; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 10px; color: #000; border: 1px dashed #ccc; margin: 20px 0;'>
                                {otp}
                            </div>
                            <p style='color: #666;'>Mã này sẽ hết hạn sau thời gian quy định.</p>
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

        // 7. hồ sơ tài khoản (chỉ cho phép khi đã login)
        [HttpGet]
        public ActionResult ProfileUser()
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int maKH = (int)Session["MaKH"];
            var user = db.KHACHHANGs.Find(maKH);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProfileUser(KHACHHANG model)
        {
            if (Session["MaKH"] == null) return RedirectToAction("Login", "Account");

            try
            {
                var user = db.KHACHHANGs.Find(model.MaKH);
                if (user != null)
                {
                    user.HoTen = model.HoTen;
                    user.DienThoai = model.DienThoai;
                    user.DiaChi = model.DiaChi;
                    user.GioiTinh = model.GioiTinh;

                    db.SaveChanges();

                    Session["HoTen"] = user.HoTen;

                    ViewBag.Success = "Cập nhật thông tin thành công!";
                    return View(user);
                }
            }
            catch (Exception)
            {
                ViewBag.Error = "Có lỗi xảy ra khi cập nhật!";
            }
            return View(model);
        }

        // 8. quên mật khẩu - nhập email
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string Email)
        {
            var user = db.KHACHHANGs.FirstOrDefault(k => k.Email == Email);
            if (user != null)
            {
                string otp = new Random().Next(100000, 999999).ToString();

                user.MaXacNhan = otp;
                user.ThoiGianHetHanOTP = DateTime.Now.AddMinutes(5);
                db.SaveChanges();

                SendEmail(user.Email, otp);

                return RedirectToAction("ResetPassword", new { email = user.Email });
            }

            ViewBag.Error = "Email này chưa được đăng ký trong hệ thống!";
            return View();
        }

        // 9. đặt lại mật khẩu mới (xác thực otp)
        [HttpGet]
        public ActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string email, string otp, string newPassword, string confirmPassword)
        {
            ViewBag.Email = email;

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            if (newPassword.Length < 8)
            {
                ViewBag.Error = "Mật khẩu quá yếu! Phải từ 8 ký tự trở lên.";
                return View();
            }

            var user = db.KHACHHANGs.FirstOrDefault(k => k.Email == email && k.MaXacNhan == otp);

            if (user != null)
            {
                if (user.ThoiGianHetHanOTP < DateTime.Now)
                {
                    ViewBag.Error = "Mã OTP đã hết hạn! Vui lòng quay lại bước Quên mật khẩu.";
                    return View();
                }

                user.MatKhau = newPassword;
                user.MaXacNhan = null;
                db.SaveChanges();

                TempData["Success"] = "Lấy lại mật khẩu thành công! Mời Sếp đăng nhập.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "Mã OTP không chính xác!";
            return View();
        }
    }
}