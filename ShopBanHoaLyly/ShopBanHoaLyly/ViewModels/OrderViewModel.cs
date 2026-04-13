using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace ShopBanHoaLyly.ViewModels
{
    public class OrderViewModel
    {
        // Thông tin người đặt
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        [StringLength(255)]
        [Display(Name = "Tên người nhận")]
        public string NguoiNhan { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(11, MinimumLength = 10, ErrorMessage = "Số điện thoại không hợp lệ.")]
        [RegularExpression(@"^(0?)(3[2-9]|5[6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])[0-9]{7}$", ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam.")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }
        
        // Địa chỉ giao hàng
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }
        
        // Tỉnh/Thành phố cố định là Đà Nẵng
        [Display(Name = "Tỉnh/Thành phố")]
        public string TinhThanhPho => "Đà Nẵng"; // Thuộc tính chỉ đọc
        
        [Required(ErrorMessage = "Vui lòng chọn quận/huyện")]
        [Display(Name = "Quận/Huyện")]
        public int MaQuanHuyen { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn phường/xã")]
        [Display(Name = "Phường/Xã")]
        public int MaPhuongXa { get; set; }
        
        // Phương thức thanh toán
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public string PhuongThucThanhToan { get; set; } = "COD"; // Mặc định là COD
        
        // Ghi chú
        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        public string? GhiChu { get; set; }
        
        // Danh sách sản phẩm
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        
        // SelectList cho dropdown
        public List<SelectListItem> QuanHuyenList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PhuongXaList { get; set; } = new List<SelectListItem>();
        
        // Tính toán
        public decimal TamTinh => CartItems != null ? CartItems.Sum(x => (x.GiaBan ?? 0) * x.SoLuong) : 0;
        
        // Phí vận chuyển tính theo phương pháp 2
        public decimal PhiVanChuyen 
        { 
            get
            {
                // Miễn phí vận chuyển cho đơn hàng từ 500.000₫ trở lên
                if (TamTinh >= 500000)
                    return 0;
                
                // Đơn hàng dưới 500.000₫: phí 30.000₫
                return 30000;
            }
        }
        
        // Tổng cộng đơn hàng (đảm bảo không nhỏ hơn 5.000 VND)
        public decimal TongCong 
        { 
            get 
            {
                decimal total = TamTinh + PhiVanChuyen;
                return total;
            }
        }
    }
} 