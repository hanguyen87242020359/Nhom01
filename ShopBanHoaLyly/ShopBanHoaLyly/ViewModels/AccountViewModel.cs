using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShopBanHoaLyly.Models;

namespace ShopBanHoaLyly.ViewModels
{
    public class AccountViewModel
    {
        [Display(Name = "Mã tài khoản")]
        public int MaTaiKhoan { get; set; }
        [Display(Name = "Tên tài khoản")]
        public string TenTaiKhoan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(255)]
        [Display(Name = "Họ và tên")]
        public string HoVaTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(11, MinimumLength = 10, ErrorMessage = "Số điện thoại không hợp lệ.")]
        [RegularExpression(@"^(0?)(3[2-9]|5[6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])[0-9]{7}$", ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam.")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết.")]
        [StringLength(255)]
        [Display(Name = "Địa chỉ chi tiết")]
        public string DiaChi { get; set; }

        // IDs để lưu trữ lựa chọn
        [Required(ErrorMessage = "Vui lòng chọn quận/huyện.")]
        [Display(Name = "Quận/Huyện")]
        public int MaQuanHuyen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phường/xã.")]
        [Display(Name = "Phường/Xã")]
        public int MaPhuongXa { get; set; }

        // Thuộc tính để hiển thị tên (không phải ID)
        [Display(Name = "Phường/Xã")]
        public string TenPhuongXa { get; set; }

        [Display(Name = "Quận/Huyện")]
        public string TenQuanHuyen { get; set; }

        // Tỉnh/Thành phố cố định là Đà Nẵng
        [Display(Name = "Tỉnh/Thành phố")]
        public string TenTinhThanh => "Đà Nẵng"; // Thuộc tính chỉ đọc

        // Danh sách cho dropdowns
        public List<SelectListItem> QuanHuyenList { get; set; } = new();
        public List<SelectListItem> PhuongXaList { get; set; } = new();

        // Thông tin mật khẩu
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string MatKhauHienTai { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
        public string MatKhauMoi { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string XacNhanMatKhauMoi { get; set; }
        
        // Danh sách đơn hàng của người dùng
        public List<DonHang> DanhSachDonHang { get; set; } = new();
    }
}