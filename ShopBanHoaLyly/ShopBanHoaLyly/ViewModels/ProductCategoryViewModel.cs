using ShopBanHoaLyly.Models;

namespace ShopBanHoaLyly.ViewModels
{
    public class ProductCategoryViewModel
    {
        public List<SanPham> products { get; set; }
        public List<DanhMuc> categories { get; set; }
    }
}
