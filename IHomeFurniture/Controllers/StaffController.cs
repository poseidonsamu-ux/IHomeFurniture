using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace IHomeFurniture.Controllers
{
    // Dùng để hiển thị dữ liệu SQL trong các trang Razor.
    public class StaffRow
    {
        private readonly Dictionary<string, object> data;

        public StaffRow(Dictionary<string, object> values)
        {
            data = values;
        }

        public string T(string key)
        {
            object value;

            if (!data.TryGetValue(key, out value) ||
                value == null ||
                value == DBNull.Value)
                return "";

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public int N(string key)
        {
            int n;
            return int.TryParse(T(key), out n) ? n : 0;
        }

        public decimal Money(string key)
        {
            decimal n;

            return decimal.TryParse(
                T(key),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out n
            ) ? n : 0m;
        }

        public string Vnd(string key)
        {
            return Money(key).ToString("N0") + " đ";
        }

        public string Date(string key)
        {
            object value;

            if (data.TryGetValue(key, out value) &&
                value is DateTime)
            {
                return ((DateTime)value).ToString("dd/MM/yyyy HH:mm");
            }

            return "—";
        }

        public bool Yes(string key)
        {
            string v = T(key);

            return v == "1" ||
                v.Equals("True", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class StaffController : Controller
    {
        private int StaffId
        {
            get
            {
                return Convert.ToInt32(Session["IHomeStaffId"]);
            }
        }

        private static SqlParameter P(string name, object value)
        {
            return new SqlParameter(
                name,
                value ?? DBNull.Value
            );
        }

        private static SqlConnection OpenDb()
        {
            var setting = ConfigurationManager
                .ConnectionStrings["IHomeStaffSql"];

            if (setting == null)
            {
                setting = ConfigurationManager
                    .ConnectionStrings["IHomeFurnitureEntities"];
            }

            if (setting == null)
                throw new ConfigurationErrorsException(
                    "Không tìm thấy connection string IHomeFurnitureEntities."
                );

            string connection = setting.ConnectionString;

            if (connection.IndexOf(
                "metadata=",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var builder = new DbConnectionStringBuilder();
                builder.ConnectionString = connection;

                object provider;

                if (!builder.TryGetValue(
                    "provider connection string", out provider))
                {
                    throw new ConfigurationErrorsException(
                        "Connection string Entity Framework không hợp lệ."
                    );
                }

                connection = Convert.ToString(provider);
            }

            var db = new SqlConnection(connection);
            db.Open();

            return db;
        }

        private static SqlCommand Cmd(
            SqlConnection db,
            SqlTransaction tx,
            string sql,
            params SqlParameter[] parameters)
        {
            var cmd = new SqlCommand(sql, db, tx);
            cmd.CommandTimeout = 30;

            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            return cmd;
        }

        private static List<StaffRow> Query(
            string sql,
            params SqlParameter[] parameters)
        {
            var result = new List<StaffRow>();

            using (var db = OpenDb())
            using (var cmd = Cmd(db, null, sql, parameters))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var dict = new Dictionary<string, object>(
                        StringComparer.OrdinalIgnoreCase
                    );

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        dict[reader.GetName(i)] =
                            reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }

                    result.Add(new StaffRow(dict));
                }
            }

            return result;
        }

        private static object Scalar(
            string sql,
            params SqlParameter[] parameters)
        {
            using (var db = OpenDb())
            using (var cmd = Cmd(db, null, sql, parameters))
            {
                return cmd.ExecuteScalar();
            }
        }

        private static object TxScalar(
            SqlConnection db,
            SqlTransaction tx,
            string sql,
            params SqlParameter[] parameters)
        {
            using (var cmd = Cmd(db, tx, sql, parameters))
            {
                return cmd.ExecuteScalar();
            }
        }

        private static int TxExecute(
            SqlConnection db,
            SqlTransaction tx,
            string sql,
            params SqlParameter[] parameters)
        {
            using (var cmd = Cmd(db, tx, sql, parameters))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        private static void Log(
            SqlConnection db,
            SqlTransaction tx,
            int staffId,
            string type,
            int itemId,
            string action,
            string description)
        {
            TxExecute(
                db, tx,
                @"INSERT INTO dbo.NHATKYNHANVIEN
                    (MaAd, Loai, MaDoiTuong, HanhDong, NoiDung)
                  VALUES
                    (@staff, @type, @item, @action, @description)",
                P("@staff", staffId),
                P("@type", type),
                P("@item", itemId),
                P("@action", action),
                P("@description", description)
            );
        }

        private static string ShortText(string value)
        {
            value = (value ?? "").Trim();

            return value.Length > 80
                ? value.Substring(0, 80)
                : value;
        }

        // ===============================
        // PHÂN QUYỀN NHÂN VIÊN
        // ===============================

        protected override void OnActionExecuting(
            ActionExecutingContext context)
        {
            string action = context.ActionDescriptor.ActionName;

            if (action.Equals(
                "DangNhap",
                StringComparison.OrdinalIgnoreCase))
            {
                base.OnActionExecuting(context);
                return;
            }

            int id;

            if (Session == null ||
                !int.TryParse(
                    Convert.ToString(Session["IHomeStaffId"]),
                    out id) ||
                id <= 0)
            {
                context.Result = RedirectToAction("DangNhap");
                return;
            }

            int valid = Convert.ToInt32(
                Scalar(
                    @"SELECT COUNT(1)
                      FROM dbo.ADMIN
                      WHERE MaAd=@id
                        AND Quyen=1
                        AND TrangThai=1",
                    P("@id", id)
                )
            );

            if (valid != 1)
            {
                Session.Remove("IHomeStaffId");
                Session.Remove("IHomeStaffName");

                context.Result = RedirectToAction("DangNhap");
                return;
            }

            base.OnActionExecuting(context);
        }

        // ===============================
        // MẬT KHẨU
        // ===============================

        private static bool EqualBytes(byte[] a, byte[] b)
        {
            int diff = a.Length ^ b.Length;

            for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                byte x = i < a.Length ? a[i] : (byte)0;
                byte y = i < b.Length ? b[i] : (byte)0;

                diff |= x ^ y;
            }

            return diff == 0;
        }

        private static string HashPassword(string password)
        {
            byte[] salt = new byte[16];

            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                120000,
                HashAlgorithmName.SHA256))
            {
                return "PBKDF2$120000$" +
                    Convert.ToBase64String(salt) + "$" +
                    Convert.ToBase64String(pbkdf2.GetBytes(32));
            }
        }

        private static bool VerifyPassword(
            string password,
            string stored,
            out bool legacy)
        {
            legacy = false;

            if (string.IsNullOrEmpty(stored))
                return false;

            if (!stored.StartsWith("PBKDF2$"))
            {
                legacy = true;

                return EqualBytes(
                    Encoding.UTF8.GetBytes(password),
                    Encoding.UTF8.GetBytes(stored)
                );
            }

            try
            {
                var parts = stored.Split('$');

                if (parts.Length != 4)
                    return false;

                int rounds = int.Parse(parts[1]);

                if (rounds < 10000 || rounds > 1000000)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);

                if (salt.Length != 16 || expected.Length != 32)
                    return false;

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    rounds,
                    HashAlgorithmName.SHA256))
                {
                    return EqualBytes(
                        expected,
                        pbkdf2.GetBytes(32)
                    );
                }
            }
            catch
            {
                return false;
            }
        }

        // ===============================
        // ĐĂNG NHẬP / ĐĂNG XUẤT
        // ===============================

        [HttpGet]
        public ActionResult DangNhap()
        {
            if (Session["IHomeStaffId"] != null)
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(
            string username,
            string password)
        {
            username = (username ?? "").Trim();

            if (username.Length == 0 ||
                username.Length > 50 ||
                string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đủ thông tin.";
                return View();
            }

            DateTime? locked = Session["StaffLockedUntil"] as DateTime?;

            if (locked.HasValue && locked.Value > DateTime.UtcNow)
            {
                ViewBag.Error = "Thử lại sau 10 phút.";
                return View();
            }

            var user = Query(
                @"SELECT TOP 1
                    MaAd, HoTen, MatKhau
                  FROM dbo.ADMIN
                  WHERE TenDN=@username
                    AND Quyen=1
                    AND TrangThai=1",
                P("@username", username)
            ).FirstOrDefault();

            bool legacy = false;

            bool valid = user != null &&
                VerifyPassword(
                    password,
                    user.T("MatKhau"),
                    out legacy
                );

            if (!valid)
            {
                int attempts = Convert.ToInt32(
                    Session["StaffLoginAttempts"] ?? 0
                ) + 1;

                if (attempts >= 5)
                {
                    Session["StaffLockedUntil"] =
                        DateTime.UtcNow.AddMinutes(10);

                    attempts = 0;
                }

                Session["StaffLoginAttempts"] = attempts;

                ViewBag.Error =
                    "Sai thông tin đăng nhập hoặc không có quyền Staff.";

                return View();
            }

            if (legacy)
            {
                using (var db = OpenDb())
                using (var tx = db.BeginTransaction())
                {
                    try
                    {
                        TxExecute(
                            db, tx,
                            @"UPDATE dbo.ADMIN
                              SET MatKhau=@new
                              WHERE MaAd=@id AND MatKhau=@old",
                            P("@new", HashPassword(password)),
                            P("@old", user.T("MatKhau")),
                            P("@id", user.N("MaAd"))
                        );

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            Session.Remove("StaffLockedUntil");
            Session.Remove("StaffLoginAttempts");

            Session["IHomeStaffId"] = user.N("MaAd");
            Session["IHomeStaffName"] = user.T("HoTen");

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangXuat()
        {
            Session.Remove("IHomeStaffId");
            Session.Remove("IHomeStaffName");

            return RedirectToAction("DangNhap");
        }

        // ===============================
        // DASHBOARD
        // ===============================

        public ActionResult Index()
        {
            ViewBag.SoDonCho = Convert.ToInt32(
                Scalar(
                    @"SELECT COUNT(*)
                      FROM dbo.DONDATHANG WHERE MaTT=1"
                )
            );

            ViewBag.SoDonDangXuLy = Convert.ToInt32(
                Scalar(
                    @"SELECT COUNT(*)
                      FROM dbo.DONDATHANG
                      WHERE MaTT IN (2,3)"
                )
            );

            ViewBag.SapHetHang = Convert.ToInt32(
                Scalar(
                    @"SELECT COUNT(*)
                      FROM dbo.SANPHAM
                      WHERE ISNULL(SoLuongTon,0)<=5
                        AND TrangThai=1"
                )
            );

            ViewBag.LienHeMoi = Convert.ToInt32(
                Scalar(
                    @"SELECT COUNT(*)
                      FROM dbo.LIENHE WHERE TrangThai=0"
                )
            );

            ViewBag.DoanhThu = Convert.ToDecimal(
                Scalar(
                    @"SELECT ISNULL(SUM(TongTien),0)
                      FROM dbo.DONDATHANG
                      WHERE MaTT=4
                        AND NgayDat>=DATEFROMPARTS(
                            YEAR(GETDATE()),
                            MONTH(GETDATE()),1
                        )
                        AND NgayDat<DATEADD(
                            MONTH,1,
                            DATEFROMPARTS(
                                YEAR(GETDATE()),
                                MONTH(GETDATE()),1
                            )
                        )"
                )
            );

            var model = Query(
                @"SELECT TOP 8
                    d.MaDonHang,
                    d.NgayDat,
                    d.TongTien,
                    d.MaTT,
                    ISNULL(k.HoTen,N'Khách vãng lai') HoTen,
                    t.TenTrangThai
                  FROM dbo.DONDATHANG d
                  LEFT JOIN dbo.KHACHHANG k ON k.MaKH=d.MaKH
                  LEFT JOIN dbo.TRANGTHAIDONHANG t
                    ON t.MaTT=d.MaTT
                  ORDER BY d.NgayDat DESC,d.MaDonHang DESC"
            );

            return View(model);
        }

        // ===============================
        // QUẢN LÝ ĐƠN HÀNG
        // ===============================

        public ActionResult QuanLyDonHang(
            string q = "",
            int trangThai = 0,
            int page = 1)
        {
            q = ShortText(q);
            page = Math.Max(1, Math.Min(page, 10000));

            ViewBag.Search = q;
            ViewBag.TrangThai = trangThai;
            ViewBag.Page = page;

            string where = @"
                FROM dbo.DONDATHANG d
                LEFT JOIN dbo.KHACHHANG k
                    ON k.MaKH=d.MaKH
                LEFT JOIN dbo.TRANGTHAIDONHANG t
                    ON t.MaTT=d.MaTT
                LEFT JOIN dbo.PHUONGTHUCTHANHTOAN p
                    ON p.MaPT=d.MaPT
                WHERE (@status=0 OR d.MaTT=@status)
                  AND (
                    @q=''
                    OR k.HoTen LIKE N'%'+@q+'%'
                    OR k.DienThoai LIKE '%'+@q+'%'
                    OR CONVERT(VARCHAR(20),d.MaDonHang)
                       LIKE '%'+@q+'%'
                  )";

            ViewBag.Total = Convert.ToInt32(
                Scalar(
                    "SELECT COUNT(*) " + where,
                    P("@status", trangThai),
                    P("@q", q)
                )
            );

            var model = Query(
                @"SELECT
                    d.MaDonHang,
                    d.NgayDat,
                    d.TongTien,
                    d.MaTT,
                    d.DienThoaiNhan,
                    ISNULL(k.HoTen,N'Khách vãng lai') HoTen,
                    t.TenTrangThai,
                    p.TenPhuongThuc " +
                where +
                @" ORDER BY d.NgayDat DESC,d.MaDonHang DESC
                   OFFSET @skip ROWS FETCH NEXT 15 ROWS ONLY",
                P("@status", trangThai),
                P("@q", q),
                P("@skip", (page - 1) * 15)
            );

            return View(model);
        }

        public ActionResult ChiTietDonHang(int id)
        {
            var order = Query(
                @"SELECT
                    d.*,
                    ISNULL(k.HoTen,N'Khách vãng lai') HoTen,
                    k.Email,
                    t.TenTrangThai,
                    p.TenPhuongThuc
                  FROM dbo.DONDATHANG d
                  LEFT JOIN dbo.KHACHHANG k
                    ON k.MaKH=d.MaKH
                  LEFT JOIN dbo.TRANGTHAIDONHANG t
                    ON t.MaTT=d.MaTT
                  LEFT JOIN dbo.PHUONGTHUCTHANHTOAN p
                    ON p.MaPT=d.MaPT
                  WHERE d.MaDonHang=@id",
                P("@id", id)
            ).FirstOrDefault();

            if (order == null)
                return HttpNotFound();

            ViewBag.ChiTiet = Query(
                @"SELECT
                    c.MaSP,
                    c.SoLuong,
                    c.DonGia,
                    c.SoLuong * c.DonGia AS ThanhTien,
                    s.TenSP,
                    s.AnhBia
                  FROM dbo.CHITIETDATHANG c
                  LEFT JOIN dbo.SANPHAM s
                    ON s.MaSP=c.MaSP
                  WHERE c.MaDonHang=@id",
                P("@id", id)
            );

            ViewBag.NhatKy = Query(
                @"SELECT TOP 20
                    n.HanhDong,
                    n.NoiDung,
                    n.NgayThucHien,
                    a.HoTen
                  FROM dbo.NHATKYNHANVIEN n
                  JOIN dbo.ADMIN a ON a.MaAd=n.MaAd
                  WHERE n.Loai=N'Đơn hàng'
                    AND n.MaDoiTuong=@id
                  ORDER BY n.MaNK DESC",
                P("@id", id)
            );

            return View(order);
        }

        private static bool CanChangeStatus(int oldValue, int newValue)
        {
            return
                (oldValue == 1 && (newValue == 2 || newValue == 5)) ||
                (oldValue == 2 && (newValue == 3 || newValue == 5)) ||
                (oldValue == 3 && newValue == 4) ||
                (oldValue == 4 && newValue == 6);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatDonHang(
            int id,
            int trangThai,
            string ghiChu)
        {
            ghiChu = (ghiChu ?? "").Trim();

            if (ghiChu.Length > 400)
            {
                TempData["Error"] = "Ghi chú tối đa 400 ký tự.";
                return RedirectToAction("ChiTietDonHang", new { id });
            }

            using (var db = OpenDb())
            using (var tx = db.BeginTransaction())
            {
                try
                {
                    object value = TxScalar(
                        db, tx,
                        @"SELECT MaTT
                          FROM dbo.DONDATHANG WITH(UPDLOCK,ROWLOCK)
                          WHERE MaDonHang=@id",
                        P("@id", id)
                    );

                    if (value == null || value == DBNull.Value)
                    {
                        tx.Rollback();
                        return HttpNotFound();
                    }

                    int oldStatus = Convert.ToInt32(value);

                    if (!CanChangeStatus(oldStatus, trangThai))
                    {
                        tx.Rollback();

                        TempData["Error"] =
                            "Không thể chuyển sang trạng thái này.";

                        return RedirectToAction(
                            "ChiTietDonHang",
                            new { id }
                        );
                    }

                    TxExecute(
                        db, tx,
                        @"UPDATE dbo.DONDATHANG
                          SET MaTT=@status,
                              NgayGiao=CASE
                                WHEN @status=4 THEN GETDATE()
                                ELSE NgayGiao
                              END
                          WHERE MaDonHang=@id",
                        P("@status", trangThai),
                        P("@id", id)
                    );

                    Log(
                        db, tx,
                        StaffId,
                        "Đơn hàng",
                        id,
                        "Chuyển trạng thái đơn hàng",
                        "Từ " + oldStatus +
                        " sang " + trangThai +
                        (ghiChu.Length > 0
                            ? " - " + ghiChu
                            : "")
                    );

                    tx.Commit();

                    TempData["Success"] =
                        "Cập nhật trạng thái đơn hàng thành công.";
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            return RedirectToAction(
                "ChiTietDonHang",
                new { id }
            );
        }

        // ===============================
        // THEO DÕI SẢN PHẨM / KHO
        // ===============================

        public ActionResult TheoDoiSanPham(
            string q = "",
            bool sapHet = false)
        {
            q = ShortText(q);

            ViewBag.Search = q;
            ViewBag.SapHet = sapHet;

            var model = Query(
                @"SELECT TOP 200
                    s.MaSP,
                    s.TenSP,
                    s.AnhBia,
                    s.SoLuongTon,
                    s.GiaBan,
                    s.GiaKhuyenMai,
                    s.TrangThai,
                    d.TenDanhMuc
                  FROM dbo.SANPHAM s
                  LEFT JOIN dbo.DANHMUC d ON d.MaDM=s.MaDM
                  WHERE (
                    @q=''
                    OR s.TenSP LIKE N'%'+@q+'%'
                    OR CONVERT(VARCHAR(20),s.MaSP)
                        LIKE '%'+@q+'%'
                  )
                  AND (
                    @low=0
                    OR ISNULL(s.SoLuongTon,0)<=5
                  )
                  ORDER BY
                    ISNULL(s.SoLuongTon,0),s.MaSP",
                P("@q", q),
                P("@low", sapHet ? 1 : 0)
            );

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatTonKho(
            int id,
            int soLuong,
            string lyDo)
        {
            lyDo = (lyDo ?? "").Trim();

            if (soLuong < 0 ||
                soLuong > 1000000 ||
                lyDo.Length < 5 ||
                lyDo.Length > 200)
            {
                TempData["Error"] =
                    "Số lượng phải từ 0 đến 1.000.000, lý do từ 5 đến 200 ký tự.";

                return RedirectToAction("TheoDoiSanPham");
            }

            using (var db = OpenDb())
            using (var tx = db.BeginTransaction())
            {
                try
                {
                    object old = TxScalar(
                        db, tx,
                        @"SELECT SoLuongTon
                          FROM dbo.SANPHAM WITH(UPDLOCK,ROWLOCK)
                          WHERE MaSP=@id",
                        P("@id", id)
                    );

                    if (old == null)
                    {
                        tx.Rollback();
                        return HttpNotFound();
                    }

                    int oldStock = old == DBNull.Value
                        ? 0
                        : Convert.ToInt32(old);

                    TxExecute(
                        db, tx,
                        @"UPDATE dbo.SANPHAM
                          SET SoLuongTon=@quantity,
                              NgayCapNhat=GETDATE()
                          WHERE MaSP=@id",
                        P("@quantity", soLuong),
                        P("@id", id)
                    );

                    Log(
                        db, tx,
                        StaffId,
                        "Kho",
                        id,
                        "Điều chỉnh số lượng tồn",
                        oldStock + " → " + soLuong +
                        ". Lý do: " + lyDo
                    );

                    tx.Commit();

                    TempData["Success"] =
                        "Đã cập nhật tồn kho thành công.";
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            return RedirectToAction("TheoDoiSanPham");
        }

        // ===============================
        // QUẢN LÝ KHÁCH HÀNG
        // ===============================

        public ActionResult QuanLyKhachHang(string q = "")
        {
            q = ShortText(q);
            ViewBag.Search = q;

            var model = Query(
                @"SELECT TOP 200
                    k.MaKH,
                    k.HoTen,
                    k.TaiKhoan,
                    k.Email,
                    k.DienThoai,
                    k.DiaChi,
                    k.NgayTao,
                    k.TrangThai,
                    COUNT(d.MaDonHang) AS SoDon,
                    ISNULL(
                        SUM(
                            CASE WHEN d.MaTT=4
                            THEN d.TongTien ELSE 0 END
                        ),0
                    ) AS DaMua
                  FROM dbo.KHACHHANG k
                  LEFT JOIN dbo.DONDATHANG d
                    ON d.MaKH=k.MaKH
                  WHERE (
                    @q=''
                    OR k.HoTen LIKE N'%'+@q+'%'
                    OR k.Email LIKE '%'+@q+'%'
                    OR k.DienThoai LIKE '%'+@q+'%'
                  )
                  GROUP BY
                    k.MaKH,
                    k.HoTen,
                    k.TaiKhoan,
                    k.Email,
                    k.DienThoai,
                    k.DiaChi,
                    k.NgayTao,
                    k.TrangThai
                  ORDER BY k.MaKH DESC",
                P("@q", q)
            );

            return View(model);
        }

        // ===============================
        // TƯ VẤN KHÁCH HÀNG
        // ===============================

        public ActionResult TuVanKhachHang(
            int trangThai = 0)
        {
            ViewBag.TrangThai = trangThai;

            var model = Query(
                @"SELECT TOP 200
                    l.MaLH,
                    l.HoTen,
                    l.Email,
                    l.DienThoai,
                    l.TieuDe,
                    l.NoiDung,
                    l.NgayGui,
                    l.TrangThai,
                    l.GhiChuXuLy,
                    l.NgayXuLy,
                    a.HoTen AS TenNhanVien
                  FROM dbo.LIENHE l
                  LEFT JOIN dbo.ADMIN a
                    ON a.MaAd=l.MaNVXuLy
                  WHERE @status=0
                     OR (@status=1 AND l.TrangThai=0)
                     OR (@status=2 AND l.TrangThai=1)
                  ORDER BY l.TrangThai,l.NgayGui DESC",
                P("@status", trangThai)
            );

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XuLyLienHe(
            int id,
            string ghiChu,
            bool daXuLy)
        {
            ghiChu = (ghiChu ?? "").Trim();

            if (ghiChu.Length < 5 || ghiChu.Length > 1000)
            {
                TempData["Error"] =
                    "Ghi chú phải có từ 5 đến 1.000 ký tự.";

                return RedirectToAction("TuVanKhachHang");
            }

            using (var db = OpenDb())
            using (var tx = db.BeginTransaction())
            {
                try
                {
                    object old = TxScalar(
                        db, tx,
                        @"SELECT TrangThai
                          FROM dbo.LIENHE WITH(UPDLOCK,ROWLOCK)
                          WHERE MaLH=@id",
                        P("@id", id)
                    );

                    if (old == null)
                    {
                        tx.Rollback();
                        return HttpNotFound();
                    }

                    TxExecute(
                        db, tx,
                        @"UPDATE dbo.LIENHE
                          SET TrangThai=@status,
                              GhiChuXuLy=@note,
                              MaNVXuLy=@staff,
                              NgayXuLy=GETDATE()
                          WHERE MaLH=@id",
                        P("@status", daXuLy),
                        P("@note", ghiChu),
                        P("@staff", StaffId),
                        P("@id", id)
                    );

                    Log(
                        db, tx,
                        StaffId,
                        "Liên hệ",
                        id,
                        daXuLy
                            ? "Hoàn tất xử lý liên hệ"
                            : "Cập nhật / mở lại liên hệ",
                        ghiChu
                    );

                    tx.Commit();

                    TempData["Success"] =
                        "Đã lưu thông tin xử lý liên hệ.";
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            return RedirectToAction("TuVanKhachHang");
        }
    }
}