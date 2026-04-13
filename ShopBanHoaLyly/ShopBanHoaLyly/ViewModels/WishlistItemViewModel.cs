namespace ShopBanHoaLyly.ViewModels
{
    public class WishlistItemViewModel
    {
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public decimal? GiaBan { get; set; }
        public string? HinhAnhDaiDien { get; set; }
        public DateTime? NgayThem { get; set; }
    }
}