using System;

namespace ShopBanHoaLyly.ViewModels
{
    public class OrderSummaryViewModel
    {
        public int MaDonHang { get; set; }
        public DateTime NgayDatHang { get; set; }
        public string TrangThaiDonHang { get; set; }
        public decimal TongTien { get; set; }
        public string SoDienThoaiNhan { get; set; }
        public string NguoiNhan { get; set; }
    }
} 