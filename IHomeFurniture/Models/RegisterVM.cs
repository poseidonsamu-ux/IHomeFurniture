using System.ComponentModel.DataAnnotations;

namespace IHomeFurniture.Models
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(0|84)(3|5|7|8|9)([0-9]{8})$", ErrorMessage = "Số điện thoại không đúng định dạng")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Tài khoản không được để trống")]
        // Ràng buộc: Không bắt đầu bằng số, không ký tự đặc biệt, ít nhất 8 ký tự
        [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9]{7,}$", ErrorMessage = "Tài khoản từ 8 ký tự, không bắt đầu bằng số, không ký tự đặc biệt")]
        public string TaiKhoan { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        // ĐÃ SỬA LUẬT: Bắt buộc 8 ký tự, 1 Hoa, 1 Thường, 1 Số, CÓ ÍT NHẤT 1 KÝ TỰ ĐẶC BIỆT
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$", ErrorMessage = "Mật khẩu ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ít nhất 1 ký tự đặc biệt (VD: @, #, $,...)")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; }

        [Compare("MatKhau", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [DataType(DataType.Password)]
        public string NhapLaiMatKhau { get; set; }
    }
}