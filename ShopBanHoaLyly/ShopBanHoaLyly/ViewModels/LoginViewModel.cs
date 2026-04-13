using System.ComponentModel.DataAnnotations;

namespace ShopBanHoaLyly.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tài khoản không được để trống.")]
        [Display(Name = "Tài khoản")]
        public string TaiKhoan { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        public bool RememberMe { get; set; }
    }
}