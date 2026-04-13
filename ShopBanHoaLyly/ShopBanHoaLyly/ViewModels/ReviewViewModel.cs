using System.ComponentModel.DataAnnotations;

namespace ShopBanHoaLyly.ViewModels
{
    public class ReviewViewModel
    {
        public int MaSanPham { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [MinLength(5, ErrorMessage = "Nội dung đánh giá phải có ít nhất 5 ký tự")]
        [MaxLength(500, ErrorMessage = "Nội dung đánh giá không được vượt quá 500 ký tự")]
        public string NoiDung { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá")]
        [Range(1, 5, ErrorMessage = "Số sao đánh giá phải từ 1 đến 5")]
        public int SoSao { get; set; }
    }
} 