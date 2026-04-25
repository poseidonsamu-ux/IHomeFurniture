using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using IHomeFurniture.Models;
using Newtonsoft.Json;

namespace IHomeFurniture.Controllers
{
    public class ChatBoxAiController : Controller
    {
        IHomeFurnitureEntities ketNoiCoSoDuLieu = new IHomeFurnitureEntities();

        private readonly string maBaoMatApi = "AIzaSyB5kMDoS95UAlGj6B5CpWKKwn086NR5bg8";

        [HttpPost]
        public async Task<JsonResult> GuiTinNhan(string cauHoi)
        {
            try
            {
                // 1. Lấy dữ liệu 20 sản phẩm bán chạy nhất
                var danhSachSanPham = ketNoiCoSoDuLieu.SANPHAMs
                    .Where(sanPham => sanPham.TrangThai == true)
                    .Select(sanPham => new { sanPham.TenSP, sanPham.GiaBan, sanPham.SoLuongTon, sanPham.MoTa })
                    .Take(20)
                    .ToList();

                string chuoiDuLieuSanPham = JsonConvert.SerializeObject(danhSachSanPham);

                // 2. BƠM TRI THỨC: Dạy con Bot mọi thứ về cửa hàng của Sếp
                string thongTinCuaHang = @"
                - Tên cửa hàng: Nội thất iHome.
                - Slogan: Không gian sống nuôi dưỡng tâm hồn.
                - Địa chỉ showroom: Khu vực Đại học Thủ Dầu Một (TDMU), Bình Dương.
                - Hotline liên hệ: 0987.654.321.
                - Chính sách giao hàng: Miễn phí vận chuyển bán kính 5km. Ngoại thành phí ship đồng giá 50.000 VNĐ. Giao nhanh trong 36H.
                - Chính sách bảo hành: 1 đổi 1 trong 7 ngày nếu lỗi từ nhà sản xuất. Bảo hành khung gỗ 12 tháng.
                - Phong cách xưng hô: Xưng 'em' hoặc 'mình', gọi khách là 'anh/chị' hoặc 'bạn'. Phải luôn lịch sự, nhiệt tình, thỉnh thoảng dùng các biểu tượng cảm xúc (như 🛋️, ✨, 😊, 🌿).
                - Định dạng tiền tệ: Luôn viết giá tiền có dấu chấm phân cách và thêm chữ VNĐ ở cuối (Ví dụ: 4.500.000 VNĐ).
                - Xử lý câu hỏi ngoài lề: Nếu khách hỏi những thứ không liên quan đến nội thất hoặc cửa hàng, hãy khéo léo từ chối và lái câu chuyện về lại các sản phẩm nội thất.
                ";

                // 3. Gộp tất cả lại thành một mệnh lệnh tối cao
                string lenhHeThong = $"Bạn là nhân viên bán hàng xuất sắc nhất của iHome Furniture.\n\n[THÔNG TIN CỬA HÀNG VÀ CHÍNH SÁCH]\n{thongTinCuaHang}\n\n[KHO HÀNG HIỆN TẠI]\n{chuoiDuLieuSanPham}\n\nHãy dựa vào lịch sử trò chuyện để tư vấn, giải đáp thắc mắc và cố gắng thuyết phục khách mua hàng một cách tự nhiên nhất.";

                if (Session["LichSuChat"] == null)
                {
                    Session["LichSuChat"] = new List<object>();
                }

                var lichSuNhanTin = (List<object>)Session["LichSuChat"];

                // Tránh để lịch sử chat quá dài làm tràn bộ nhớ API (Giữ lại 10 tin nhắn gần nhất)
                if (lichSuNhanTin.Count > 10)
                {
                    lichSuNhanTin.RemoveRange(0, lichSuNhanTin.Count - 10);
                }

                lichSuNhanTin.Add(new { role = "user", parts = new[] { new { text = cauHoi } } });

                string cauTraLoiTuAi = await GoiApiAI(lenhHeThong, lichSuNhanTin);

                lichSuNhanTin.Add(new { role = "model", parts = new[] { new { text = cauTraLoiTuAi } } });
                Session["LichSuChat"] = lichSuNhanTin;

                return Json(new { traloi = cauTraLoiTuAi });
            }
            catch (Exception loiHeThong)
            {
                return Json(new { traloi = "Hệ thống đang bận xíu, sếp thử lại sau nha: " + loiHeThong.Message });
            }
        }

        private async Task<string> GoiApiAI(string lenhHeThong, List<object> lichSuNhanTin)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (HttpClient mayKhachHttp = new HttpClient())
            {
                string duongDanApi = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + maBaoMatApi.Trim();

                var duLieuDongGoi = new
                {
                    system_instruction = new { parts = new[] { new { text = lenhHeThong } } },
                    contents = lichSuNhanTin
                };

                string chuoiJsonGuiDi = JsonConvert.SerializeObject(duLieuDongGoi);
                HttpContent noiDungGuiDi = new StringContent(chuoiJsonGuiDi, Encoding.UTF8, "application/json");

                HttpResponseMessage phanHoiTuGoogle = await mayKhachHttp.PostAsync(duongDanApi, noiDungGuiDi);
                string ketQuaTraVe = await phanHoiTuGoogle.Content.ReadAsStringAsync();

                if (!phanHoiTuGoogle.IsSuccessStatusCode)
                {
                    return "Lỗi từ Google: " + ketQuaTraVe;
                }

                dynamic doiTuongJson = JsonConvert.DeserializeObject(ketQuaTraVe);
                return doiTuongJson.candidates[0].content.parts[0].text;
            }
        }
    }
}