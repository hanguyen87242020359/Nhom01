using System;

namespace ShopBanHoaLyly.ViewModels
{
    public class CartItemViewModel
    {
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public decimal? GiaBan { get; set; }
        public int SoLuong { get; set; }
        public string? HinhAnhDaiDien { get; set; }
        public decimal? TongCong => GiaBan * SoLuong;
        public bool ChonMua { get; set; } // tick chọn mua
        public int? SoLuongCon { get; set; }
    }
}
