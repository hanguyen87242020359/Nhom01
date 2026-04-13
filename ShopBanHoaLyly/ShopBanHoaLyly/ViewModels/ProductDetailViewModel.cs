using System;
using System.Collections.Generic;
using ShopBanHoaLyly.Models;

namespace ShopBanHoaLyly.ViewModels
{
    public class ProductDetailViewModel
    {
        public SanPham Product { get; set; }
        public IEnumerable<HinhAnh> Images { get; set; }
        public IEnumerable<DanhGia> Reviews { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public IEnumerable<SanPham> RelatedProducts { get; set; }
        
        // Thêm các thuộc tính cho chức năng đánh giá
        public bool UserHasPurchased { get; set; }
        public bool UserHasReviewed { get; set; }
        public ReviewViewModel NewReview { get; set; }
    }
}